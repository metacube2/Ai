"""
Inference Module für Modell-Evaluation
Lädt Base- und Fine-tuned Models für Vergleiche
"""

from pathlib import Path
from typing import Optional, Dict
import threading


class ModelInference:
    """Handhabt Modell-Inferenz für Base und Fine-tuned Models"""

    def __init__(self, models_dir: str = "models", output_dir: str = "output"):
        self.models_dir = Path(models_dir)
        self.output_dir = Path(output_dir)

        self.base_model = None
        self.finetuned_model = None
        self.model_lock = threading.Lock()

    def load_base_model(self, model_name: str) -> bool:
        """Lädt das Basis-Modell"""
        try:
            # Import MLX nur bei Bedarf
            from mlx_lm import load

            model_path = self.models_dir / model_name

            if not model_path.exists():
                return False

            with self.model_lock:
                self.base_model = load(str(model_path))

            return True

        except Exception as e:
            print(f"Error loading base model: {e}")
            return False

    def load_finetuned_model(self, model_name: str, adapter_path: str) -> bool:
        """Lädt das Fine-tuned Modell (Base + LoRA Adapter)"""
        try:
            from mlx_lm import load

            model_path = self.models_dir / model_name
            adapter_file = Path(adapter_path)

            if not model_path.exists() or not adapter_file.exists():
                return False

            with self.model_lock:
                # Lade Base Model mit Adapter
                self.finetuned_model = load(
                    str(model_path),
                    adapter_path=str(adapter_file)
                )

            return True

        except Exception as e:
            print(f"Error loading finetuned model: {e}")
            return False

    def generate(self, prompt: str, model_type: str = 'base',
                max_tokens: int = 512, temperature: float = 0.7) -> str:
        """
        Generiert Text mit dem gewählten Modell

        Args:
            prompt: Input prompt
            model_type: 'base' oder 'finetuned'
            max_tokens: Maximale Anzahl Tokens
            temperature: Sampling temperature

        Returns:
            Generierter Text
        """
        try:
            from mlx_lm import generate as mlx_generate

            model = self.base_model if model_type == 'base' else self.finetuned_model

            if model is None:
                return f"Error: {model_type} model not loaded"

            with self.model_lock:
                # MLX-LM generate
                result = mlx_generate(
                    model,
                    prompt=prompt,
                    max_tokens=max_tokens,
                    temp=temperature
                )

            return result

        except Exception as e:
            return f"Error during generation: {str(e)}"

    def generate_comparison(self, prompt: str, max_tokens: int = 512,
                          temperature: float = 0.7) -> Dict[str, str]:
        """
        Generiert mit beiden Modellen für Vergleich

        Returns:
            Dict mit 'base' und 'finetuned' Outputs
        """
        result = {
            'base': None,
            'finetuned': None
        }

        if self.base_model:
            result['base'] = self.generate(
                prompt, 'base', max_tokens, temperature
            )

        if self.finetuned_model:
            result['finetuned'] = self.generate(
                prompt, 'finetuned', max_tokens, temperature
            )

        return result

    def format_mail_prompt(self, task_type: str, mail_body: str) -> str:
        """Formatiert einen Prompt basierend auf Task-Type"""

        task_prompts = {
            'Zusammenfassen': 'Fasse folgende E-Mail zusammen:',
            'Antwort schreiben': 'Schreibe eine Antwort auf folgende E-Mail:',
            'Kategorisieren': 'Kategorisiere folgende E-Mail:',
            'Action Items': 'Extrahiere die Action Items aus folgender E-Mail:',
            'Custom': 'Bearbeite folgende E-Mail:'
        }

        instruction = task_prompts.get(task_type, task_prompts['Custom'])

        return f"{instruction}\n\n{mail_body}"

    def get_test_prompts(self) -> Dict[str, str]:
        """Vordefinierte Test-Prompts"""
        return {
            'Zusammenfassen': self.format_mail_prompt(
                'Zusammenfassen',
                """Betreff: Q4 Projektupdate

Hallo Team,

ich wollte euch ein kurzes Update zum aktuellen Projektstand geben.

Wir haben letzte Woche die neue API-Integration abgeschlossen und erfolgreich getestet.
Die Performance-Tests zeigen eine Verbesserung von 40% gegenüber der alten Implementierung.

Nächste Woche starten wir mit der Frontend-Anpassung. Maria und Tom werden das Design
überarbeiten, während ich mich um die Backend-Anbindung kümmere.

Der Go-Live ist weiterhin für Ende des Monats geplant.

Beste Grüße
Alex"""
            ),
            'Antwort schreiben': self.format_mail_prompt(
                'Antwort schreiben',
                """Betreff: Frage zu Invoice #2847

Hallo,

ich habe eine Frage zur Rechnung #2847 vom 15. März.
Der Betrag scheint nicht mit unserem Angebot übereinzustimmen.

Könnten Sie das bitte prüfen?

Danke
Michael"""
            ),
            'Action Items': self.format_mail_prompt(
                'Action Items',
                """Betreff: Meeting Notes - Produktlaunch

Hi alle,

hier die wichtigsten Punkte vom heutigen Meeting:

- Sarah bereitet die Pressemitteilung vor (Deadline: Freitag)
- Marketing-Team erstellt Social Media Content (nächste Woche)
- Ich kümmere mich um die Influencer-Kontakte
- Wir brauchen noch finale Produktfotos vom Design-Team
- Launch-Event ist am 1. April - Location muss noch gebucht werden

Bitte gebt bis Mittwoch Bescheid ob ihr eure Aufgaben schaffen könnt.

Lisa"""
            )
        }

    def unload_models(self):
        """Entlädt Modelle aus dem Speicher"""
        with self.model_lock:
            self.base_model = None
            self.finetuned_model = None

    def get_loaded_models(self) -> Dict[str, bool]:
        """Gibt zurück welche Modelle geladen sind"""
        return {
            'base': self.base_model is not None,
            'finetuned': self.finetuned_model is not None
        }
