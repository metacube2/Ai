// Mail Fine-Tuning App - Frontend Logic

const API_BASE = '';

// State
let currentMails = [];
let currentLabelingIndex = 0;
let stats = {};
let trainingEventSource = null;

// ======================
// Utility Functions
// ======================

function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 4000);
}

async function apiCall(endpoint, options = {}) {
    try {
        const response = await fetch(API_BASE + endpoint, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            }
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.detail || 'API Error');
        }

        return await response.json();
    } catch (error) {
        showToast(error.message, 'error');
        throw error;
    }
}

// ======================
// Navigation
// ======================

function initNavigation() {
    const navLinks = document.querySelectorAll('.nav-link');
    const views = document.querySelectorAll('.view');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            const targetView = link.dataset.view;

            // Update active states
            navLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');

            views.forEach(v => v.classList.remove('active'));
            document.getElementById(`${targetView}-view`).classList.add('active');

            // Load data for view
            if (targetView === 'labeling') {
                loadLabelingView();
            } else if (targetView === 'export') {
                loadStats();
            } else if (targetView === 'models') {
                loadModels();
            } else if (targetView === 'training') {
                loadTrainingView();
            }
        });
    });
}

// ======================
// Mail Import
// ======================

function initImport() {
    const dropzone = document.getElementById('dropzone');
    const fileInput = document.getElementById('file-input');

    dropzone.addEventListener('click', () => fileInput.click());

    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
    });

    dropzone.addEventListener('dragleave', () => {
        dropzone.classList.remove('dragover');
    });

    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
        handleFiles(e.dataTransfer.files);
    });

    fileInput.addEventListener('change', (e) => {
        handleFiles(e.target.files);
    });

    document.getElementById('refresh-mails').addEventListener('click', loadMails);

    // Initial load
    loadMails();
}

async function handleFiles(files) {
    const formData = new FormData();

    for (let file of files) {
        formData.append('files', file);
    }

    try {
        const response = await fetch(API_BASE + '/api/mails/upload', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        const successCount = result.success.reduce((sum, r) => sum + r.count, 0);
        showToast(`${successCount} Mails erfolgreich importiert`, 'success');

        if (result.errors.length > 0) {
            showToast(`${result.errors.length} Fehler beim Import`, 'error');
        }

        loadMails();

    } catch (error) {
        showToast('Fehler beim Upload', 'error');
    }
}

async function loadMails() {
    try {
        const data = await apiCall('/api/mails');
        currentMails = data.mails;

        document.getElementById('mail-count').textContent = currentMails.length;

        renderMailList(currentMails);
    } catch (error) {
        console.error('Error loading mails:', error);
    }
}

function renderMailList(mails) {
    const container = document.getElementById('mail-list');

    if (mails.length === 0) {
        container.innerHTML = '<p style="text-align:center; padding: 2rem;">Keine Mails vorhanden</p>';
        return;
    }

    container.innerHTML = mails.map(mail => `
        <div class="mail-item ${mail.status}">
            <div class="mail-header">
                <div class="mail-subject">${escapeHtml(mail.subject)}</div>
                <div class="mail-meta">${mail.status}</div>
            </div>
            <div class="mail-meta">Von: ${escapeHtml(mail.sender)}</div>
            <div class="mail-body">${escapeHtml(mail.body)}</div>
            <div class="mail-actions">
                <button class="btn btn-secondary" onclick="viewMail(${mail.id})">👁️ Ansehen</button>
                <button class="btn btn-danger" onclick="deleteMail(${mail.id})">🗑️ Löschen</button>
            </div>
        </div>
    `).join('');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function deleteMail(id) {
    if (!confirm('Mail wirklich löschen?')) return;

    try {
        await apiCall(`/api/mails/${id}`, { method: 'DELETE' });
        showToast('Mail gelöscht', 'success');
        loadMails();
    } catch (error) {
        console.error('Error deleting mail:', error);
    }
}

function viewMail(id) {
    const mail = currentMails.find(m => m.id === id);
    if (!mail) return;

    alert(`Betreff: ${mail.subject}\n\nVon: ${mail.sender}\n\n${mail.body}`);
}

// ======================
// Labeling
// ======================

function initLabeling() {
    const statusFilter = document.getElementById('status-filter');
    statusFilter.addEventListener('change', loadLabelingView);

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        const activeView = document.querySelector('.view.active');
        if (activeView.id !== 'labeling-view') return;

        if (e.key.toLowerCase() === 'n') {
            nextMail();
        } else if (e.key.toLowerCase() === 's') {
            saveLabelingMail();
        } else if (e.key.toLowerCase() === 'k') {
            skipMail();
        }
    });
}

async function loadLabelingView() {
    const statusFilter = document.getElementById('status-filter').value;

    try {
        const data = await apiCall(`/api/mails?status=${statusFilter || ''}`);
        currentMails = data.mails;
        currentLabelingIndex = 0;

        updateLabelingProgress();
        renderCurrentMail();
    } catch (error) {
        console.error('Error loading labeling view:', error);
    }
}

function updateLabelingProgress() {
    const labeled = currentMails.filter(m => m.status === 'labeled').length;
    const total = currentMails.length;

    const percent = total > 0 ? (labeled / total) * 100 : 0;

    document.getElementById('labeling-progress').style.width = `${percent}%`;
    document.getElementById('progress-text').textContent = `${labeled} / ${total} gelabelt`;
}

function renderCurrentMail() {
    const container = document.getElementById('labeling-container');

    if (currentMails.length === 0) {
        container.innerHTML = '<p>Keine Mails zum Labeln vorhanden</p>';
        return;
    }

    const mail = currentMails[currentLabelingIndex];

    container.innerHTML = `
        <div class="current-mail">
            <h4>${escapeHtml(mail.subject)}</h4>
            <p><strong>Von:</strong> ${escapeHtml(mail.sender)}</p>
            <p><strong>An:</strong> ${escapeHtml(mail.recipient)}</p>
            <hr style="margin: 1rem 0; border-color: var(--border-color)">
            <div style="white-space: pre-wrap;">${escapeHtml(mail.body)}</div>
        </div>

        <form id="labeling-form">
            <div class="form-group">
                <label>Aufgabentyp:</label>
                <select id="task-type" required>
                    <option value="">-- Wählen --</option>
                    <option value="Zusammenfassen" ${mail.task_type === 'Zusammenfassen' ? 'selected' : ''}>Zusammenfassen</option>
                    <option value="Antwort schreiben" ${mail.task_type === 'Antwort schreiben' ? 'selected' : ''}>Antwort schreiben</option>
                    <option value="Kategorisieren" ${mail.task_type === 'Kategorisieren' ? 'selected' : ''}>Kategorisieren</option>
                    <option value="Action Items" ${mail.task_type === 'Action Items' ? 'selected' : ''}>Action Items</option>
                    <option value="Custom" ${mail.task_type === 'Custom' ? 'selected' : ''}>Custom</option>
                </select>
            </div>

            <div class="form-group">
                <label>Erwarteter Output:</label>
                <textarea id="expected-output" rows="6" required>${mail.expected_output || ''}</textarea>
            </div>

            <div class="form-actions">
                <button type="button" class="btn btn-primary" onclick="saveLabelingMail()">💾 Speichern (S)</button>
                <button type="button" class="btn btn-secondary" onclick="skipMail()">⏭️ Überspringen (K)</button>
                <button type="button" class="btn btn-secondary" onclick="nextMail()">➡️ Nächste (N)</button>
                <span style="margin-left: auto; color: var(--text-secondary);">
                    ${currentLabelingIndex + 1} / ${currentMails.length}
                </span>
            </div>
        </form>
    `;
}

async function saveLabelingMail() {
    const mail = currentMails[currentLabelingIndex];
    const taskType = document.getElementById('task-type').value;
    const expectedOutput = document.getElementById('expected-output').value;

    if (!taskType || !expectedOutput) {
        showToast('Bitte alle Felder ausfüllen', 'warning');
        return;
    }

    try {
        await apiCall(`/api/mails/${mail.id}`, {
            method: 'PUT',
            body: JSON.stringify({
                task_type: taskType,
                expected_output: expectedOutput,
                status: 'labeled'
            })
        });

        showToast('Gespeichert', 'success');
        mail.status = 'labeled';
        updateLabelingProgress();
        nextMail();
    } catch (error) {
        console.error('Error saving mail:', error);
    }
}

async function skipMail() {
    const mail = currentMails[currentLabelingIndex];

    try {
        await apiCall(`/api/mails/${mail.id}`, {
            method: 'PUT',
            body: JSON.stringify({
                status: 'skip'
            })
        });

        mail.status = 'skip';
        updateLabelingProgress();
        nextMail();
    } catch (error) {
        console.error('Error skipping mail:', error);
    }
}

function nextMail() {
    if (currentLabelingIndex < currentMails.length - 1) {
        currentLabelingIndex++;
    } else {
        currentLabelingIndex = 0;
    }
    renderCurrentMail();
}

// ======================
// Export & Stats
// ======================

function initExport() {
    document.getElementById('export-jsonl').addEventListener('click', exportJSONL);
}

async function loadStats() {
    try {
        stats = await apiCall('/api/export/stats');
        renderStats();
    } catch (error) {
        console.error('Error loading stats:', error);
    }
}

function renderStats() {
    const container = document.getElementById('stats-grid');

    container.innerHTML = `
        <div class="stat-card">
            <div class="stat-value">${stats.total || 0}</div>
            <div class="stat-label">Gesamt Mails</div>
        </div>
        <div class="stat-card">
            <div class="stat-value">${stats.labeled || 0}</div>
            <div class="stat-label">Gelabelt</div>
        </div>
        <div class="stat-card">
            <div class="stat-value">${stats.unlabeled || 0}</div>
            <div class="stat-label">Unlabeled</div>
        </div>
        <div class="stat-card">
            <div class="stat-value">${stats.avg_input_length || 0}</div>
            <div class="stat-label">Avg Input Length</div>
        </div>
        <div class="stat-card">
            <div class="stat-value">${stats.avg_output_length || 0}</div>
            <div class="stat-label">Avg Output Length</div>
        </div>
        <div class="stat-card">
            <div class="stat-value">${stats.sufficient_data ? '✅' : '❌'}</div>
            <div class="stat-label">Genug Daten (&gt;50)</div>
        </div>
    `;
}

async function exportJSONL() {
    const trainSplit = document.getElementById('train-split').value / 100;

    try {
        const result = await apiCall('/api/export/jsonl', {
            method: 'POST',
            body: JSON.stringify({ train_split: trainSplit })
        });

        const resultDiv = document.getElementById('export-result');
        resultDiv.innerHTML = `
            <p>✅ Export erfolgreich!</p>
            <p>Training Samples: ${result.train_samples}</p>
            <p>Validation Samples: ${result.val_samples}</p>
            <p>
                <a href="/api/export/download/train" class="btn btn-primary" download>📥 train.jsonl</a>
                <a href="/api/export/download/val" class="btn btn-primary" download>📥 val.jsonl</a>
            </p>
        `;
        resultDiv.classList.add('show');

        showToast('JSONL Dateien generiert', 'success');
    } catch (error) {
        console.error('Error exporting JSONL:', error);
    }
}

// ======================
// Models
// ======================

async function loadModels() {
    try {
        const data = await apiCall('/api/models');
        renderModels(data.models);
    } catch (error) {
        console.error('Error loading models:', error);
    }
}

function renderModels(models) {
    const container = document.getElementById('models-list');

    if (models.length === 0) {
        container.innerHTML = '<p>Keine Modelle vorhanden</p>';
        return;
    }

    container.innerHTML = models.map(model => `
        <div class="model-item">
            <span>📦 ${model}</span>
            <span style="color: var(--accent-success);">✓ Verfügbar</span>
        </div>
    `).join('');
}

// ======================
// Training
// ======================

function initTraining() {
    const lrSlider = document.getElementById('learning-rate');
    const epochsSlider = document.getElementById('epochs');

    lrSlider.addEventListener('input', (e) => {
        const value = Math.pow(10, parseFloat(e.target.value));
        document.getElementById('lr-value').textContent = value.toExponential(0);
    });

    epochsSlider.addEventListener('input', (e) => {
        document.getElementById('epochs-value').textContent = e.target.value;
    });

    document.getElementById('training-form').addEventListener('submit', startTraining);
    document.getElementById('stop-training').addEventListener('click', stopTraining);
}

async function loadTrainingView() {
    // Load available models
    try {
        const data = await apiCall('/api/models');
        const select = document.getElementById('training-model');

        select.innerHTML = '<option value="">-- Modell wählen --</option>' +
            data.models.map(m => `<option value="${m}">${m}</option>`).join('');
    } catch (error) {
        console.error('Error loading models:', error);
    }

    // Get current status
    updateTrainingStatus();
}

async function startTraining(e) {
    e.preventDefault();

    const modelName = document.getElementById('training-model').value;
    const learningRate = Math.pow(10, parseFloat(document.getElementById('learning-rate').value));
    const epochs = parseInt(document.getElementById('epochs').value);
    const batchSize = parseInt(document.getElementById('batch-size').value);
    const loraRank = parseInt(document.getElementById('lora-rank').value);

    if (!modelName) {
        showToast('Bitte Modell wählen', 'warning');
        return;
    }

    try {
        await apiCall('/api/training/start', {
            method: 'POST',
            body: JSON.stringify({
                model_name: modelName,
                learning_rate: learningRate,
                epochs: epochs,
                batch_size: batchSize,
                lora_rank: loraRank
            })
        });

        showToast('Training gestartet', 'success');

        document.getElementById('start-training').disabled = true;
        document.getElementById('stop-training').disabled = false;

        // Start SSE stream
        startTrainingStream();

    } catch (error) {
        console.error('Error starting training:', error);
    }
}

async function stopTraining() {
    try {
        await apiCall('/api/training/stop', { method: 'POST' });
        showToast('Training gestoppt', 'warning');

        document.getElementById('start-training').disabled = false;
        document.getElementById('stop-training').disabled = true;

        if (trainingEventSource) {
            trainingEventSource.close();
        }
    } catch (error) {
        console.error('Error stopping training:', error);
    }
}

function startTrainingStream() {
    if (trainingEventSource) {
        trainingEventSource.close();
    }

    trainingEventSource = new EventSource('/api/training/stream');

    trainingEventSource.onmessage = (event) => {
        const status = JSON.parse(event.data);
        updateTrainingStatusUI(status);

        if (!status.is_training && status.current_step > 0) {
            trainingEventSource.close();
            document.getElementById('start-training').disabled = false;
            document.getElementById('stop-training').disabled = true;
            showToast('Training abgeschlossen', 'success');
        }
    };

    trainingEventSource.onerror = () => {
        trainingEventSource.close();
    };
}

async function updateTrainingStatus() {
    try {
        const status = await apiCall('/api/training/status');
        updateTrainingStatusUI(status);

        if (status.is_training) {
            document.getElementById('start-training').disabled = true;
            document.getElementById('stop-training').disabled = false;
            startTrainingStream();
        }
    } catch (error) {
        console.error('Error updating status:', error);
    }
}

function updateTrainingStatusUI(status) {
    const container = document.getElementById('training-status');

    if (!status.is_training && status.current_step === 0) {
        container.innerHTML = '<p>Kein Training aktiv</p>';
        return;
    }

    const eta = status.eta_seconds ? `${Math.floor(status.eta_seconds / 60)}m ${status.eta_seconds % 60}s` : 'N/A';

    container.innerHTML = `
        <div class="status-grid">
            <div class="status-item">
                <label>Status</label>
                <div class="value">${status.is_training ? '🟢 Running' : '⏸️ Stopped'}</div>
            </div>
            <div class="status-item">
                <label>Step</label>
                <div class="value">${status.current_step} / ${status.total_steps}</div>
            </div>
            <div class="status-item">
                <label>Epoch</label>
                <div class="value">${status.current_epoch}</div>
            </div>
            <div class="status-item">
                <label>Train Loss</label>
                <div class="value">${status.train_loss || 'N/A'}</div>
            </div>
            <div class="status-item">
                <label>Val Loss</label>
                <div class="value">${status.val_loss || 'N/A'}</div>
            </div>
            <div class="status-item">
                <label>ETA</label>
                <div class="value">${eta}</div>
            </div>
            <div class="status-item">
                <label>Memory</label>
                <div class="value">${status.memory_usage_percent}%</div>
            </div>
        </div>
    `;

    // Update charts (simple implementation without chart library)
    updateChart('train-loss-chart', status.train_loss_history);
    updateChart('val-loss-chart', status.val_loss_history);
}

function updateChart(canvasId, data) {
    // Simplified chart rendering (without external library)
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    canvas.width = canvas.offsetWidth;
    canvas.height = 200;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (!data || data.length === 0) return;

    const padding = 20;
    const width = canvas.width - 2 * padding;
    const height = canvas.height - 2 * padding;

    const maxVal = Math.max(...data);
    const minVal = Math.min(...data);
    const range = maxVal - minVal || 1;

    ctx.strokeStyle = '#4a9eff';
    ctx.lineWidth = 2;
    ctx.beginPath();

    data.forEach((val, i) => {
        const x = padding + (i / (data.length - 1)) * width;
        const y = padding + height - ((val - minVal) / range) * height;

        if (i === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    });

    ctx.stroke();
}

// ======================
// Evaluation
// ======================

function initEvaluation() {
    document.getElementById('load-test-prompt').addEventListener('click', loadTestPrompt);
    document.getElementById('run-comparison').addEventListener('click', runComparison);
}

async function loadTestPrompt() {
    const taskType = document.getElementById('eval-task-type').value;

    try {
        const prompts = await apiCall('/api/inference/test-prompts');
        const prompt = prompts[taskType];

        if (prompt) {
            // Extract mail body from prompt
            const parts = prompt.split('\n\n');
            document.getElementById('eval-mail-text').value = parts.slice(1).join('\n\n');
            showToast('Test-Beispiel geladen', 'success');
        }
    } catch (error) {
        console.error('Error loading test prompt:', error);
    }
}

async function runComparison() {
    const taskType = document.getElementById('eval-task-type').value;
    const mailBody = document.getElementById('eval-mail-text').value;

    if (!mailBody) {
        showToast('Bitte Mail-Text eingeben', 'warning');
        return;
    }

    document.getElementById('base-result').textContent = 'Generiere...';
    document.getElementById('finetuned-result').textContent = 'Generiere...';

    try {
        const result = await apiCall('/api/inference/compare', {
            method: 'POST',
            body: JSON.stringify({
                task_type: taskType,
                mail_body: mailBody
            })
        });

        document.getElementById('base-result').textContent = result.base || 'Modell nicht geladen';
        document.getElementById('finetuned-result').textContent = result.finetuned || 'Modell nicht geladen';

        showToast('Vergleich abgeschlossen', 'success');
    } catch (error) {
        console.error('Error running comparison:', error);
        document.getElementById('base-result').textContent = 'Fehler';
        document.getElementById('finetuned-result').textContent = 'Fehler';
    }
}

// ======================
// Init
// ======================

document.addEventListener('DOMContentLoaded', () => {
    initNavigation();
    initImport();
    initLabeling();
    initExport();
    initTraining();
    initEvaluation();
});
