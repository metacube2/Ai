"""Tkinter interface for Geometric Resonator."""

from __future__ import annotations

import math
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

from synth import SHAPE_MODES, SynthEngine


BG = "#09131c"
PANEL = "#111f2a"
CYAN = "#54e1ff"
MAGENTA = "#ff5bbd"
TEXT = "#e6f6ff"
MUTED = "#88a5b5"


class GeometricResonatorApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.engine = SynthEngine()
        self.phase = 0.0
        self.mouse_gate = False
        self.hold_var = tk.BooleanVar(value=False)
        self.status_var = tk.StringVar(value="Bereit – Objekt anklicken oder Leertaste")
        self.value_vars: dict[str, tk.StringVar] = {}

        root.title("Geometric Resonator")
        root.geometry("1080x700")
        root.minsize(920, 620)
        root.configure(bg=BG)
        root.protocol("WM_DELETE_WINDOW", self.close)

        self._configure_styles()
        self._build_ui()
        self._bind_controls()
        self._sync_engine()
        self._animate()

    def _configure_styles(self) -> None:
        style = ttk.Style()
        style.theme_use("clam")
        style.configure("TFrame", background=BG)
        style.configure("Panel.TFrame", background=PANEL)
        style.configure("TLabel", background=PANEL, foreground=TEXT)
        style.configure("Muted.TLabel", background=PANEL, foreground=MUTED)
        style.configure("TButton", padding=8)
        style.configure("TCheckbutton", background=PANEL, foreground=TEXT)
        style.map("TCheckbutton", background=[("active", PANEL)])
        style.configure("TCombobox", fieldbackground="#182d3a")

    def _build_ui(self) -> None:
        title = tk.Label(
            self.root,
            text="GEOMETRIC  RESONATOR",
            bg=BG,
            fg=TEXT,
            font=("Helvetica Neue", 24, "bold"),
        )
        title.pack(anchor="w", padx=28, pady=(22, 4))
        subtitle = tk.Label(
            self.root,
            text="Form wird Klang · Bogenreibung oder Luftstrom",
            bg=BG,
            fg=MUTED,
            font=("Helvetica Neue", 12),
        )
        subtitle.pack(anchor="w", padx=30, pady=(0, 14))

        body = ttk.Frame(self.root)
        body.pack(fill="both", expand=True, padx=24, pady=(0, 18))
        body.columnconfigure(0, weight=3)
        body.columnconfigure(1, weight=2)
        body.rowconfigure(0, weight=1)

        canvas_frame = ttk.Frame(body, style="Panel.TFrame")
        canvas_frame.grid(row=0, column=0, sticky="nsew", padx=(0, 12))
        self.canvas = tk.Canvas(
            canvas_frame, bg="#071017", highlightthickness=0, cursor="crosshair"
        )
        self.canvas.pack(fill="both", expand=True, padx=2, pady=2)
        self.canvas.bind("<ButtonPress-1>", self._mouse_down)
        self.canvas.bind("<B1-Motion>", self._mouse_drag)
        self.canvas.bind("<ButtonRelease-1>", self._mouse_up)

        panel = ttk.Frame(body, style="Panel.TFrame", padding=20)
        panel.grid(row=0, column=1, sticky="nsew", padx=(12, 0))
        panel.columnconfigure(0, weight=1)

        self.instrument_var = tk.StringVar(value="Violine")
        self.shape_var = tk.StringVar(value="Kreis")
        self._combo(panel, "Anregung", self.instrument_var, ("Violine", "Flöte"), 0)
        self._combo(panel, "Geometrie", self.shape_var, tuple(SHAPE_MODES), 2)

        self.pitch_var = tk.DoubleVar(value=220.0)
        self.size_var = tk.DoubleVar(value=100.0)
        self.excitation_var = tk.DoubleVar(value=0.55)
        self.damping_var = tk.DoubleVar(value=0.45)
        self.brightness_var = tk.DoubleVar(value=0.55)
        self.volume_var = tk.DoubleVar(value=0.35)

        row = 4
        row = self._slider(panel, "Grundton", self.pitch_var, 55, 880, row, "Hz")
        row = self._slider(panel, "Objektgröße", self.size_var, 45, 180, row, "%")
        row = self._slider(panel, "Bogen / Luftstrom", self.excitation_var, 0.05, 1.0, row)
        row = self._slider(panel, "Dämpfung", self.damping_var, 0.0, 1.0, row)
        row = self._slider(panel, "Helligkeit", self.brightness_var, 0.0, 1.0, row)
        row = self._slider(panel, "Lautstärke", self.volume_var, 0.0, 0.75, row)

        hold = ttk.Checkbutton(
            panel,
            text="Ton halten",
            variable=self.hold_var,
            command=self._hold_changed,
        )
        hold.grid(row=row, column=0, sticky="w", pady=(12, 8))
        row += 1

        buttons = ttk.Frame(panel, style="Panel.TFrame")
        buttons.grid(row=row, column=0, sticky="ew", pady=6)
        buttons.columnconfigure((0, 1), weight=1)
        self.sound_button = ttk.Button(buttons, text="Ton starten", command=self.toggle_sound)
        self.sound_button.grid(row=0, column=0, sticky="ew", padx=(0, 5))
        ttk.Button(buttons, text="WAV exportieren", command=self.export_wav).grid(
            row=0, column=1, sticky="ew", padx=(5, 0)
        )
        row += 1

        ttk.Label(panel, textvariable=self.status_var, style="Muted.TLabel", wraplength=330).grid(
            row=row, column=0, sticky="ew", pady=(12, 0)
        )

    def _combo(self, parent, label, variable, values, row) -> None:
        ttk.Label(parent, text=label).grid(row=row, column=0, sticky="w")
        combo = ttk.Combobox(parent, textvariable=variable, values=values, state="readonly")
        combo.grid(row=row + 1, column=0, sticky="ew", pady=(4, 12))
        combo.bind("<<ComboboxSelected>>", lambda _event: self._sync_engine())

    def _slider(self, parent, label, variable, minimum, maximum, row, unit="") -> int:
        header = ttk.Frame(parent, style="Panel.TFrame")
        header.grid(row=row, column=0, sticky="ew", pady=(4, 0))
        header.columnconfigure(0, weight=1)
        ttk.Label(header, text=label).grid(row=0, column=0, sticky="w")
        value_var = tk.StringVar()
        self.value_vars[label] = value_var
        ttk.Label(header, textvariable=value_var, style="Muted.TLabel").grid(
            row=0, column=1, sticky="e"
        )
        scale = ttk.Scale(
            parent,
            from_=minimum,
            to=maximum,
            variable=variable,
            command=lambda _value: self._sync_engine(),
        )
        scale.grid(row=row + 1, column=0, sticky="ew", pady=(2, 8))
        value_var.set(f"{variable.get():.2f}{unit}")
        return row + 2

    def _bind_controls(self) -> None:
        self.root.bind("<KeyPress-space>", self._space_down)
        self.root.bind("<KeyRelease-space>", self._space_up)

    def _sync_engine(self) -> None:
        self.engine.update(
            instrument=self.instrument_var.get(),
            shape=self.shape_var.get(),
            pitch_hz=self.pitch_var.get(),
            size=self.size_var.get(),
            excitation=self.excitation_var.get(),
            damping=self.damping_var.get(),
            brightness=self.brightness_var.get(),
            volume=self.volume_var.get(),
        )
        units = {
            "Grundton": "Hz",
            "Objektgröße": "%",
            "Bogen / Luftstrom": "",
            "Dämpfung": "",
            "Helligkeit": "",
            "Lautstärke": "",
        }
        variables = {
            "Grundton": self.pitch_var,
            "Objektgröße": self.size_var,
            "Bogen / Luftstrom": self.excitation_var,
            "Dämpfung": self.damping_var,
            "Helligkeit": self.brightness_var,
            "Lautstärke": self.volume_var,
        }
        for label, variable in variables.items():
            digits = 0 if label in ("Grundton", "Objektgröße") else 2
            self.value_vars[label].set(f"{variable.get():.{digits}f}{units[label]}")

    def _ensure_audio(self) -> bool:
        try:
            self.engine.start()
            return True
        except Exception as error:
            messagebox.showerror("Audio konnte nicht starten", str(error))
            self.status_var.set("Audiofehler – WAV-Export funktioniert weiterhin")
            return False

    def toggle_sound(self) -> None:
        if self.hold_var.get() and self.engine.running:
            self.hold_var.set(False)
            self.engine.set_gate(False)
            self.sound_button.configure(text="Ton starten")
            self.status_var.set("Ton gestoppt")
            return
        if not self._ensure_audio():
            return
        self.hold_var.set(True)
        self.engine.set_gate(True)
        self.sound_button.configure(text="Ton stoppen")
        self.status_var.set(f"{self.instrument_var.get()} regt {self.shape_var.get()} an")

    def _hold_changed(self) -> None:
        if self.hold_var.get() and not self._ensure_audio():
            self.hold_var.set(False)
            return
        self.engine.set_gate(self.hold_var.get())
        self.sound_button.configure(text="Ton stoppen" if self.hold_var.get() else "Ton starten")

    def _mouse_down(self, event) -> None:
        if not self._ensure_audio():
            return
        self.mouse_gate = True
        self._mouse_drag(event)
        self.engine.set_gate(True)

    def _mouse_drag(self, event) -> None:
        width = max(self.canvas.winfo_width(), 1)
        height = max(self.canvas.winfo_height(), 1)
        pitch = 55.0 * (16.0 ** max(0.0, min(1.0, event.x / width)))
        excitation = 1.0 - max(0.0, min(1.0, event.y / height))
        self.pitch_var.set(pitch)
        self.excitation_var.set(max(0.05, excitation))
        self._sync_engine()

    def _mouse_up(self, _event) -> None:
        self.mouse_gate = False
        if not self.hold_var.get():
            self.engine.set_gate(False)

    def _space_down(self, _event) -> None:
        if not self.mouse_gate and self._ensure_audio():
            self.engine.set_gate(True)

    def _space_up(self, _event) -> None:
        if not self.hold_var.get() and not self.mouse_gate:
            self.engine.set_gate(False)

    def _shape_points(self, cx, cy, radius, sides, rotation) -> list[float]:
        points: list[float] = []
        for index in range(sides):
            angle = rotation + index * 2.0 * math.pi / sides
            points.extend((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
        return points

    def _animate(self) -> None:
        canvas = self.canvas
        width, height = canvas.winfo_width(), canvas.winfo_height()
        if width > 20 and height > 20:
            canvas.delete("all")
            cx, cy = width / 2, height / 2
            params = self.engine.snapshot()
            base = min(width, height) * 0.22 * params.size / 100.0
            pulse = 1.0 + self.engine.level * 1.8
            radius = min(base * pulse, min(width, height) * 0.39)
            self.phase += 0.007 + self.engine.level * 0.04

            canvas.create_line(30, cy, width - 30, cy, fill="#123040")
            canvas.create_line(cx, 30, cx, height - 30, fill="#123040")
            for ring in range(1, 5):
                ring_radius = radius + ring * 16 + math.sin(self.phase * 5 - ring) * self.engine.level * 90
                canvas.create_oval(
                    cx - ring_radius,
                    cy - ring_radius,
                    cx + ring_radius,
                    cy + ring_radius,
                    outline="#12384a",
                    width=1,
                )

            color = MAGENTA if params.instrument == "Violine" else CYAN
            if params.shape == "Kreis":
                canvas.create_oval(
                    cx - radius,
                    cy - radius,
                    cx + radius,
                    cy + radius,
                    outline=color,
                    width=4,
                    fill="#0d2430",
                )
            else:
                sides = {"Dreieck": 3, "Quadrat": 4, "Sechseck": 6}[params.shape]
                rotation = self.phase - math.pi / 2
                canvas.create_polygon(
                    self._shape_points(cx, cy, radius, sides, rotation),
                    outline=color,
                    width=4,
                    fill="#0d2430",
                    joinstyle="round",
                )

            canvas.create_text(
                cx,
                cy - 10,
                text=f"{params.frequency:.1f} Hz",
                fill=TEXT,
                font=("Helvetica Neue", 22, "bold"),
            )
            canvas.create_text(
                cx,
                cy + 22,
                text=f"{params.shape} · {params.instrument}",
                fill=MUTED,
                font=("Helvetica Neue", 12),
            )
            canvas.create_text(
                cx,
                height - 28,
                text="Horizontal: Tonhöhe   ·   Vertikal: Anregung",
                fill=MUTED,
                font=("Helvetica Neue", 11),
            )
        self.root.after(30, self._animate)

    def export_wav(self) -> None:
        initial = f"{self.shape_var.get()}-{self.instrument_var.get()}.wav"
        path = filedialog.asksaveasfilename(
            title="Vier Sekunden Klang exportieren",
            initialfile=initial,
            defaultextension=".wav",
            filetypes=(("WAV Audio", "*.wav"),),
        )
        if not path:
            return
        try:
            self.engine.export_wav(path, 4.0)
            self.status_var.set(f"Exportiert: {Path(path).name}")
        except Exception as error:
            messagebox.showerror("Export fehlgeschlagen", str(error))

    def close(self) -> None:
        self.engine.stop()
        self.root.destroy()


def main() -> None:
    root = tk.Tk()
    GeometricResonatorApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()

