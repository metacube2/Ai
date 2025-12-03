"""
FastAPI Backend für Mail Fine-Tuning App
Hauptanwendung mit allen API Endpoints
"""

from fastapi import FastAPI, File, UploadFile, HTTPException, BackgroundTasks
from fastapi.responses import StreamingResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, List
import asyncio
import json
from pathlib import Path
import shutil

from data_manager import DataManager
from mail_parser import MailParser
from training import MLXTrainer, TrainingConfig
from inference import ModelInference

# FastAPI App
app = FastAPI(title="Mail Fine-Tuning App")

# CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialisiere Manager
data_manager = DataManager("data/mails.db")
trainer = MLXTrainer("models", "output")
inference = ModelInference("models", "output")


# Pydantic Models
class MailUpdate(BaseModel):
    task_type: Optional[str] = None
    expected_output: Optional[str] = None
    status: Optional[str] = None
    body: Optional[str] = None


class TrainingStartRequest(BaseModel):
    model_name: str
    learning_rate: float = 1e-5
    epochs: int = 3
    batch_size: int = 4
    lora_rank: int = 8


class InferenceRequest(BaseModel):
    prompt: str
    model_type: str = 'base'
    max_tokens: int = 512
    temperature: float = 0.7


class InferenceComparisonRequest(BaseModel):
    task_type: str
    mail_body: str
    max_tokens: int = 512
    temperature: float = 0.7


# ===== Mail Endpoints =====

@app.post("/api/mails/upload")
async def upload_mails(files: List[UploadFile] = File(...)):
    """Upload und Parse von Mail-Dateien"""
    results = {
        'success': [],
        'errors': []
    }

    for file in files:
        try:
            # Temporär speichern
            temp_path = Path("data/temp") / file.filename
            temp_path.parent.mkdir(parents=True, exist_ok=True)

            with open(temp_path, 'wb') as f:
                content = await file.read()
                f.write(content)

            # Parse Mails
            parsed_mails = MailParser.parse_file(temp_path)

            # In DB speichern
            for mail in parsed_mails:
                mail_id = data_manager.add_mail(
                    subject=mail['subject'],
                    sender=mail['sender'],
                    recipient=mail['recipient'],
                    date=mail['date'],
                    body=mail['body'],
                    original_format=mail['original_format']
                )

            results['success'].append({
                'filename': file.filename,
                'count': len(parsed_mails)
            })

            # Cleanup
            temp_path.unlink()

        except Exception as e:
            results['errors'].append({
                'filename': file.filename,
                'error': str(e)
            })

    return results


@app.get("/api/mails")
async def get_mails(status: Optional[str] = None):
    """Liste aller Mails"""
    mails = data_manager.get_all_mails(status_filter=status)
    return {'mails': mails}


@app.get("/api/mails/{mail_id}")
async def get_mail(mail_id: int):
    """Einzelne Mail abrufen"""
    mail = data_manager.get_mail(mail_id)
    if not mail:
        raise HTTPException(status_code=404, detail="Mail not found")
    return mail


@app.put("/api/mails/{mail_id}")
async def update_mail(mail_id: int, update: MailUpdate):
    """Mail aktualisieren (Labeling)"""
    success = data_manager.update_mail(
        mail_id=mail_id,
        task_type=update.task_type,
        expected_output=update.expected_output,
        status=update.status,
        body=update.body
    )

    if not success:
        raise HTTPException(status_code=404, detail="Mail not found")

    return {'success': True}


@app.delete("/api/mails/{mail_id}")
async def delete_mail(mail_id: int):
    """Mail löschen"""
    success = data_manager.delete_mail(mail_id)

    if not success:
        raise HTTPException(status_code=404, detail="Mail not found")

    return {'success': True}


# ===== Export Endpoints =====

@app.get("/api/export/stats")
async def get_stats():
    """Statistiken abrufen"""
    stats = data_manager.get_statistics()
    return stats


@app.post("/api/export/jsonl")
async def export_jsonl(train_split: float = 0.9):
    """Exportiert Training-Daten als JSONL"""
    train_data, val_data = data_manager.export_training_data(train_split)

    if not train_data:
        raise HTTPException(status_code=400, detail="No labeled data available")

    # Speichere Files
    data_dir = Path("data")
    train_file = data_dir / "train.jsonl"
    val_file = data_dir / "val.jsonl"

    train_file_path, val_file_path = trainer.prepare_training_data(
        train_data, val_data, data_dir
    )

    return {
        'success': True,
        'train_samples': len(train_data),
        'val_samples': len(val_data),
        'train_file': str(train_file),
        'val_file': str(val_file)
    }


@app.get("/api/export/download/{file_type}")
async def download_file(file_type: str):
    """Download JSONL Files"""
    if file_type not in ['train', 'val']:
        raise HTTPException(status_code=400, detail="Invalid file type")

    file_path = Path("data") / f"{file_type}.jsonl"

    if not file_path.exists():
        raise HTTPException(status_code=404, detail="File not found")

    return FileResponse(
        path=file_path,
        filename=f"{file_type}.jsonl",
        media_type='application/json'
    )


# ===== Model Endpoints =====

@app.get("/api/models")
async def list_models():
    """Liste verfügbarer Modelle"""
    models = trainer.list_available_models()
    return {'models': models}


@app.post("/api/models/download")
async def download_model(model_name: str):
    """
    Lädt ein Modell herunter
    Placeholder - würde in echter Implementation huggingface nutzen
    """
    success = trainer.download_model(model_name)

    if not success:
        raise HTTPException(
            status_code=501,
            detail="Model download not implemented. Please download manually."
        )

    return {'success': True}


# ===== Training Endpoints =====

@app.post("/api/training/start")
async def start_training(request: TrainingStartRequest, background_tasks: BackgroundTasks):
    """Startet Training"""

    # Hole Training-Daten
    train_data, val_data = data_manager.export_training_data()

    if not train_data:
        raise HTTPException(status_code=400, detail="No labeled data available")

    if len(train_data) < 10:
        raise HTTPException(
            status_code=400,
            detail=f"Not enough training data. Need at least 10, got {len(train_data)}"
        )

    # Training Config
    config = TrainingConfig(
        model_name=request.model_name,
        learning_rate=request.learning_rate,
        epochs=request.epochs,
        batch_size=request.batch_size,
        lora_rank=request.lora_rank
    )

    # Starte Training
    success = trainer.start_training(config, train_data, val_data)

    if not success:
        raise HTTPException(status_code=400, detail="Training already running")

    return {'success': True, 'message': 'Training started'}


@app.post("/api/training/stop")
async def stop_training():
    """Stoppt Training"""
    success = trainer.stop_training()

    if not success:
        raise HTTPException(status_code=400, detail="No training running")

    return {'success': True, 'message': 'Training stopped'}


@app.get("/api/training/status")
async def get_training_status():
    """Gibt aktuellen Training-Status zurück"""
    status = trainer.get_status()
    return status


@app.get("/api/training/stream")
async def stream_training_status():
    """
    Server-Sent Events für Live-Updates
    """
    async def event_generator():
        while True:
            status = trainer.get_status()

            # Sende Status als SSE
            yield f"data: {json.dumps(status)}\n\n"

            # Stop wenn Training fertig
            if not status['is_training'] and status['current_step'] > 0:
                break

            await asyncio.sleep(1)

    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream"
    )


# ===== Inference Endpoints =====

@app.post("/api/inference/load")
async def load_model(model_type: str, model_name: str, adapter_path: Optional[str] = None):
    """Lädt ein Modell für Inference"""

    if model_type == 'base':
        success = inference.load_base_model(model_name)
    elif model_type == 'finetuned':
        if not adapter_path:
            raise HTTPException(status_code=400, detail="adapter_path required for finetuned model")
        success = inference.load_finetuned_model(model_name, adapter_path)
    else:
        raise HTTPException(status_code=400, detail="Invalid model_type")

    if not success:
        raise HTTPException(status_code=400, detail="Failed to load model")

    return {'success': True}


@app.get("/api/inference/loaded")
async def get_loaded_models():
    """Gibt zurück welche Modelle geladen sind"""
    loaded = inference.get_loaded_models()
    return loaded


@app.post("/api/inference/generate")
async def generate_text(request: InferenceRequest):
    """Generiert Text mit geladenem Modell"""
    result = inference.generate(
        prompt=request.prompt,
        model_type=request.model_type,
        max_tokens=request.max_tokens,
        temperature=request.temperature
    )

    return {'result': result}


@app.post("/api/inference/compare")
async def compare_models(request: InferenceComparisonRequest):
    """Vergleicht Base und Fine-tuned Model"""

    prompt = inference.format_mail_prompt(
        request.task_type,
        request.mail_body
    )

    result = inference.generate_comparison(
        prompt=prompt,
        max_tokens=request.max_tokens,
        temperature=request.temperature
    )

    return result


@app.get("/api/inference/test-prompts")
async def get_test_prompts():
    """Gibt vordefinierte Test-Prompts zurück"""
    prompts = inference.get_test_prompts()
    return prompts


# ===== Static Files =====

# Serve Frontend
app.mount("/", StaticFiles(directory="frontend", html=True), name="frontend")


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
