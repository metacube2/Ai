/**
 * Video Converter Suite - Control Panel JavaScript
 * Nuclear Power Plant Style UI Controller
 */

// ============ STATE ============
const state = {
    currentPage: 'dashboard',
    selectedFormat: 'mp4',
    selectedPreset: 'balanced',
    selectedResolution: 'original',
    uploadedFile: null,
    uploadedFilePath: null,
    jobs: [],
    streams: [],
    pipelines: [],
    activePipelineId: null,
    pipelineStages: [],
    activeStreamId: null,
    wsConnected: false,
    refreshInterval: null,
};

// ============ INIT ============
document.addEventListener('DOMContentLoaded', () => {
    initClock();
    initNavigation();
    initUploadZone();
    startAutoRefresh();
    refreshStatus();
    addLog('System initialisiert', 'info');
});

// ============ CLOCK ============
function initClock() {
    const el = document.getElementById('systemClock');
    function update() {
        const now = new Date();
        el.textContent = now.toTimeString().split(' ')[0];
    }
    update();
    setInterval(update, 1000);
}

// ============ NAVIGATION ============
function initNavigation() {
    document.querySelectorAll('.nav-tab').forEach(tab => {
        tab.addEventListener('click', () => {
            const page = tab.dataset.page;
            switchPage(page);
        });
    });
}

function switchPage(page) {
    state.currentPage = page;
    document.querySelectorAll('.nav-tab').forEach(t => t.classList.remove('active'));
    document.querySelector(`.nav-tab[data-page="${page}"]`)?.classList.add('active');
    document.querySelectorAll('.page-content').forEach(p => p.style.display = 'none');
    document.getElementById(`page-${page}`).style.display = '';

    // Refresh page-specific data
    if (page === 'dashboard') refreshStatus();
    if (page === 'streams') refreshStreams();
    if (page === 'pipelines') refreshPipelines();
    if (page === 'queue') refreshQueue();
}

// ============ UPLOAD ============
function initUploadZone() {
    const zone = document.getElementById('uploadZone');
    if (!zone) return;

    zone.addEventListener('dragover', (e) => {
        e.preventDefault();
        zone.classList.add('dragover');
    });

    zone.addEventListener('dragleave', () => {
        zone.classList.remove('dragover');
    });

    zone.addEventListener('drop', (e) => {
        e.preventDefault();
        zone.classList.remove('dragover');
        if (e.dataTransfer.files.length > 0) {
            uploadFile(e.dataTransfer.files[0]);
        }
    });
}

function handleFileSelect(event) {
    if (event.target.files.length > 0) {
        uploadFile(event.target.files[0]);
    }
}

async function uploadFile(file) {
    state.uploadedFile = file;
    addLog(`Upload gestartet: ${file.name} (${formatBytes(file.size)})`, 'info');

    const formData = new FormData();
    formData.append('file', file);

    try {
        const resp = await fetch('/api/upload', {
            method: 'POST',
            body: formData,
        });
        const data = await resp.json();

        if (data.error) {
            addLog(`Upload-Fehler: ${data.error}`, 'error');
            notify('Upload fehlgeschlagen: ' + data.error, 'error');
            return;
        }

        state.uploadedFilePath = data.path;
        displayUploadedFile(data);
        document.getElementById('btnStartConvert').disabled = false;
        addLog(`Upload abgeschlossen: ${file.name}`, 'success');
        notify('Datei hochgeladen: ' + file.name, 'success');
    } catch (err) {
        addLog(`Upload-Fehler: ${err.message}`, 'error');
        notify('Upload fehlgeschlagen', 'error');
    }
}

function displayUploadedFile(data) {
    const el = document.getElementById('uploadedFileInfo');
    el.style.display = 'block';

    const info = data.info || {};
    const video = info.video || {};
    const audio = info.audio || {};

    el.innerHTML = `
        <div style="background:var(--bg-inset); border:1px solid var(--border-dark); border-radius:4px; padding:12px;">
            <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                <strong style="color:var(--accent-cyan);">${data.original_name}</strong>
                <span style="color:var(--text-dim); font-size:11px;">${formatBytes(data.size)}</span>
            </div>
            <div style="display:grid; grid-template-columns: 1fr 1fr; gap:8px; font-size:11px;">
                <div><span style="color:var(--text-dim)">Format:</span> ${info.format_name || 'N/A'}</div>
                <div><span style="color:var(--text-dim)">Dauer:</span> ${formatDuration(info.duration || 0)}</div>
                ${video ? `
                <div><span style="color:var(--text-dim)">Video:</span> ${video.codec || 'N/A'} ${video.width || ''}x${video.height || ''}</div>
                <div><span style="color:var(--text-dim)">FPS:</span> ${video.fps || 'N/A'}</div>
                ` : ''}
                ${audio ? `
                <div><span style="color:var(--text-dim)">Audio:</span> ${audio.codec || 'N/A'}</div>
                <div><span style="color:var(--text-dim)">Sample:</span> ${audio.sample_rate || 'N/A'} Hz</div>
                ` : ''}
            </div>
        </div>
    `;
}

// ============ FORMAT SELECTION ============
function selectFormat(format) {
    state.selectedFormat = format;
    document.querySelectorAll('.format-switch').forEach(s => s.classList.remove('selected'));
    document.querySelectorAll(`.format-switch[data-format="${format}"]`).forEach(s => s.classList.add('selected'));
    addLog(`Format gewählt: ${format.toUpperCase()}`, 'info');
}

function selectPreset(preset) {
    state.selectedPreset = preset;
    document.querySelectorAll('#presetPanel .switch-unit').forEach(s => s.classList.remove('active'));
    document.querySelector(`#presetPanel .switch-unit[data-preset="${preset}"]`)?.classList.add('active');
}

function selectResolution(res) {
    state.selectedResolution = res;
    document.querySelectorAll('#resolutionPanel .switch-unit').forEach(s => s.classList.remove('active'));
    document.querySelector(`#resolutionPanel .switch-unit[data-resolution="${res}"]`)?.classList.add('active');
}

// ============ CONVERSION ============
async function startConversion() {
    if (!state.uploadedFilePath) {
        notify('Keine Datei hochgeladen', 'warning');
        return;
    }

    const params = {
        input_file: state.uploadedFilePath,
        output_format: state.selectedFormat,
        preset: state.selectedPreset,
    };

    if (state.selectedResolution !== 'original') {
        params.resolution = state.selectedResolution;
    }

    addLog(`Konvertierung gestartet: ${state.selectedFormat.toUpperCase()} / ${state.selectedPreset}`, 'info');
    document.getElementById('conversionStatus').textContent = 'Konvertierung wird gestartet...';

    try {
        const resp = await fetch('/api/convert', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(params),
        });
        const data = await resp.json();

        if (data.error) {
            addLog(`Fehler: ${data.error}`, 'error');
            notify(data.error, 'error');
            return;
        }

        addLog(`Job erstellt: ${data.id}`, 'success');
        notify('Konvertierung gestartet', 'success');
        document.getElementById('btnStopAll').style.display = '';
        document.getElementById('conversionStatus').textContent = `Job ${data.id} läuft...`;
        startJobPolling(data.id);

    } catch (err) {
        addLog(`Fehler: ${err.message}`, 'error');
        notify('Konvertierung fehlgeschlagen', 'error');
    }
}

function startJobPolling(jobId) {
    const poll = setInterval(async () => {
        try {
            const resp = await fetch(`/api/jobs/${jobId}/progress`);
            const progress = await resp.json();

            document.getElementById('conversionStatus').textContent =
                `${progress.percent || 0}% | FPS: ${progress.fps || 0} | Speed: ${progress.speed || '0x'} | Zeit: ${progress.time || '00:00:00'}`;

            updateJobInList(jobId, progress);

            if (progress.percent >= 100) {
                clearInterval(poll);
                addLog(`Job ${jobId} abgeschlossen`, 'success');
                notify('Konvertierung abgeschlossen!', 'success');
                document.getElementById('conversionStatus').textContent = 'Konvertierung abgeschlossen!';
                refreshJobs();
            }
        } catch (e) {
            // Keep polling
        }
    }, 1000);
}

async function stopAllJobs() {
    if (!confirm('Alle laufenden Jobs stoppen?')) return;

    try {
        const resp = await fetch('/api/jobs');
        const data = await resp.json();
        for (const job of (data.jobs || [])) {
            if (job.status === 'running') {
                await fetch(`/api/jobs/${job.id}/cancel`, { method: 'POST' });
                addLog(`Job ${job.id} gestoppt`, 'warn');
            }
        }
        notify('Alle Jobs gestoppt', 'warning');
        document.getElementById('btnStopAll').style.display = 'none';
        refreshJobs();
    } catch (e) {
        notify('Fehler beim Stoppen', 'error');
    }
}

// ============ JOBS ============
async function refreshJobs() {
    try {
        const resp = await fetch('/api/jobs');
        const data = await resp.json();
        state.jobs = data.jobs || [];
        renderJobList();
    } catch (e) {
        // Silently fail
    }
}

function renderJobList() {
    const el = document.getElementById('jobList');
    if (!state.jobs.length) {
        el.innerHTML = '<div style="text-align:center; color:var(--text-dim); padding:20px;">Keine aktiven Jobs</div>';
        return;
    }

    el.innerHTML = state.jobs.map(job => `
        <div class="job-item" id="job-${job.id}">
            <div class="job-thumb">${job.thumbnail ? `<img src="${job.thumbnail}">` : '&#127910;'}</div>
            <div class="job-info">
                <div class="job-name">${job.input_file ? job.input_file.split('/').pop() : 'Unknown'}</div>
                <div class="job-meta">
                    ${job.output_format?.toUpperCase() || ''} | ${job.preset || ''} | ${job.resolution || 'Original'}
                </div>
                <div class="progress-bar" style="margin-top:6px;">
                    <div class="progress-fill" id="progress-${job.id}" style="width:${job.status === 'completed' ? 100 : 0}%"></div>
                </div>
                <div class="progress-label">
                    <span id="progress-text-${job.id}">${job.status === 'completed' ? '100%' : '0%'}</span>
                    <span id="progress-speed-${job.id}"></span>
                </div>
            </div>
            <span class="job-status ${job.status}">${job.status}</span>
            <div class="job-actions">
                ${job.status === 'running' ? `<button class="btn btn-icon btn-danger" onclick="cancelJob('${job.id}')" data-tooltip="Stop">&#9632;</button>` : ''}
                ${job.status === 'completed' ? `<button class="btn btn-icon btn-success" onclick="downloadJob('${job.id}')" data-tooltip="Download">&#8681;</button>` : ''}
                <button class="btn btn-icon btn-danger" onclick="deleteJob('${job.id}')" data-tooltip="Löschen">&#10005;</button>
            </div>
        </div>
    `).join('');

    // Update active job count
    const running = state.jobs.filter(j => j.status === 'running').length;
    document.getElementById('activeJobCount').textContent = running;
    document.getElementById('gaugeJobs').textContent = running;
}

function updateJobInList(jobId, progress) {
    const bar = document.getElementById(`progress-${jobId}`);
    const text = document.getElementById(`progress-text-${jobId}`);
    const speed = document.getElementById(`progress-speed-${jobId}`);
    if (bar) bar.style.width = `${progress.percent || 0}%`;
    if (text) text.textContent = `${progress.percent || 0}%`;
    if (speed) speed.textContent = `${progress.fps || 0} fps | ${progress.speed || ''}`;
}

async function cancelJob(id) {
    await fetch(`/api/jobs/${id}/cancel`, { method: 'POST' });
    addLog(`Job ${id} abgebrochen`, 'warn');
    refreshJobs();
}

async function deleteJob(id) {
    await fetch(`/api/jobs/${id}`, { method: 'DELETE' });
    addLog(`Job ${id} gelöscht`, 'info');
    refreshJobs();
}

function downloadJob(id) {
    window.open(`/api/download/${id}`, '_blank');
}

// ============ STREAMS ============
async function refreshStreams() {
    try {
        const resp = await fetch('/api/streams');
        const data = await resp.json();
        state.streams = data.streams || [];
        renderStreamMatrix();
        updateStreamSelect();
    } catch (e) {}
}

function renderStreamMatrix() {
    const el = document.getElementById('streamMatrix');
    if (!state.streams.length) {
        el.innerHTML = '<div style="text-align:center; color:var(--text-dim); padding:40px; grid-column:1/-1;">Keine aktiven Streams</div>';
        return;
    }

    el.innerHTML = state.streams.map(s => `
        <div class="stream-card">
            <div class="stream-preview">
                <span class="no-signal">${s.status === 'running' ? '&#9654; LIVE' : 'NO SIGNAL'}</span>
                ${s.status === 'running' ? '<span class="live-badge">LIVE</span>' : ''}
            </div>
            <div class="stream-info">
                <div class="stream-name">${s.input_url || 'Stream'}</div>
                <div style="font-size:10px; color:var(--text-dim);">
                    ${s.output_format?.toUpperCase() || ''} | ${s.resolution || 'Original'} | ${s.preset || 'fast'}
                </div>
                <span class="job-status ${s.status}" style="margin-top:6px; display:inline-block;">${s.status}</span>
            </div>
            <div class="stream-controls">
                ${s.status === 'running' ?
                    `<button class="btn btn-danger" onclick="stopStream('${s.id}')">&#9632; Stop</button>` :
                    `<button class="btn btn-success" onclick="restartStream('${s.id}')">&#9654; Restart</button>`
                }
                <button class="btn" onclick="deleteStream('${s.id}')">&#10005;</button>
            </div>
        </div>
    `).join('');
}

function updateStreamSelect() {
    const sel = document.getElementById('activeStreamSelect');
    const runningStreams = state.streams.filter(s => s.status === 'running');
    sel.innerHTML = '<option value="">-- Stream wählen --</option>' +
        runningStreams.map(s =>
            `<option value="${s.id}">${s.input_url} (${s.output_format?.toUpperCase()})</option>`
        ).join('');
}

function openStreamModal() {
    document.getElementById('streamModal').classList.add('visible');
}

function closeStreamModal() {
    document.getElementById('streamModal').classList.remove('visible');
}

async function startNewStream() {
    const inputUrl = document.getElementById('streamInputUrl').value;
    if (!inputUrl) {
        notify('Bitte Stream-URL eingeben', 'warning');
        return;
    }

    const params = {
        input_url: inputUrl,
        output_format: document.getElementById('streamOutputFormat').value,
        resolution: document.getElementById('streamResolution').value || null,
        preset: document.getElementById('streamPreset').value,
    };

    try {
        const resp = await fetch('/api/streams', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(params),
        });
        const data = await resp.json();

        if (data.error) {
            notify(data.error, 'error');
            return;
        }

        addLog(`Stream gestartet: ${data.id}`, 'success');
        notify('Stream gestartet', 'success');
        closeStreamModal();
        refreshStreams();
    } catch (e) {
        notify('Stream-Start fehlgeschlagen', 'error');
    }
}

async function stopStream(id) {
    await fetch(`/api/streams/${id}`, { method: 'DELETE' });
    addLog(`Stream ${id} gestoppt`, 'warn');
    refreshStreams();
}

async function deleteStream(id) {
    await fetch(`/api/streams/${id}`, { method: 'DELETE' });
    refreshStreams();
}

function selectActiveStream(id) {
    state.activeStreamId = id;
    // Highlight current format
    const stream = state.streams.find(s => s.id === id);
    document.querySelectorAll('[data-stream-format]').forEach(el => el.classList.remove('selected'));
    if (stream) {
        document.querySelector(`[data-stream-format="${stream.output_format}"]`)?.classList.add('selected');
    }
}

async function switchStreamFormat(format) {
    if (!state.activeStreamId) {
        notify('Bitte zuerst einen Stream wählen', 'warning');
        return;
    }

    addLog(`Format-Wechsel: ${format.toUpperCase()} für Stream ${state.activeStreamId}`, 'warn');

    try {
        const resp = await fetch(`/api/streams/${state.activeStreamId}/switch`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ format }),
        });
        const data = await resp.json();

        if (data.error) {
            notify(data.error, 'error');
            return;
        }

        addLog(`Format gewechselt zu ${format.toUpperCase()}`, 'success');
        notify(`Format umgeschaltet: ${format.toUpperCase()}`, 'success');

        // Update active stream ID to new stream
        state.activeStreamId = data.id;
        refreshStreams();

        // Highlight new format
        document.querySelectorAll('[data-stream-format]').forEach(el => el.classList.remove('selected'));
        document.querySelector(`[data-stream-format="${format}"]`)?.classList.add('selected');
    } catch (e) {
        notify('Format-Wechsel fehlgeschlagen', 'error');
    }
}

// ============ PIPELINES ============
async function refreshPipelines() {
    try {
        const resp = await fetch('/api/pipelines');
        const data = await resp.json();
        state.pipelines = data.pipelines || [];
        renderPipelineList();
    } catch (e) {}
}

function renderPipelineList() {
    const el = document.getElementById('pipelineList');
    if (!state.pipelines.length) {
        el.innerHTML = '<div style="text-align:center; color:var(--text-dim); padding:16px;">Keine Pipelines vorhanden</div>';
        return;
    }

    el.innerHTML = state.pipelines.map(p => `
        <div class="job-item" style="cursor:pointer" onclick="editPipeline('${p.id}')">
            <div class="job-thumb" style="font-size:24px">&#9776;</div>
            <div class="job-info">
                <div class="job-name">${p.name}</div>
                <div class="job-meta">${(p.stages || []).length} Stufen | Status: ${p.status}</div>
            </div>
            <span class="job-status ${p.status}">${p.status}</span>
            <div class="job-actions">
                <button class="btn btn-icon btn-primary" onclick="event.stopPropagation(); runPipeline('${p.id}')" data-tooltip="Ausführen">&#9654;</button>
                <button class="btn btn-icon btn-danger" onclick="event.stopPropagation(); deletePipeline('${p.id}')" data-tooltip="Löschen">&#10005;</button>
            </div>
        </div>
    `).join('');
}

async function createPipeline() {
    const name = prompt('Pipeline-Name:', 'Neue Pipeline');
    if (!name) return;

    try {
        const resp = await fetch('/api/pipelines', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name }),
        });
        const data = await resp.json();
        addLog(`Pipeline erstellt: ${data.name}`, 'success');
        refreshPipelines();
        editPipeline(data.id);
    } catch (e) {
        notify('Pipeline-Erstellung fehlgeschlagen', 'error');
    }
}

function editPipeline(id) {
    state.activePipelineId = id;
    const pipeline = state.pipelines.find(p => p.id === id);
    if (!pipeline) return;

    state.pipelineStages = pipeline.stages || [];
    document.getElementById('pipelineEditor').style.display = '';
    renderPipelineFlow();
}

function renderPipelineFlow() {
    const flow = document.getElementById('pipelineFlow');
    let html = `
        <div class="pipeline-node active">
            <div class="node-type">Input</div>
            <div class="node-name">Source</div>
            <div class="node-status"></div>
        </div>
    `;

    state.pipelineStages.forEach((stage, i) => {
        html += `<div class="pipeline-connector ${stage.enabled ? 'active' : ''}"></div>`;
        html += `
            <div class="pipeline-node ${stage.enabled ? 'active' : 'disabled'}" onclick="toggleStage(${i})">
                <div class="node-type">${stage.type}</div>
                <div class="node-name">${stage.label || stage.type}</div>
                <div class="node-status"></div>
            </div>
        `;
    });

    html += `<div class="pipeline-connector active"></div>`;
    html += `
        <div class="pipeline-node active">
            <div class="node-type">Output</div>
            <div class="node-name">Target</div>
            <div class="node-status"></div>
        </div>
    `;

    flow.innerHTML = html;
}

function toggleStage(index) {
    if (state.pipelineStages[index]) {
        state.pipelineStages[index].enabled = !state.pipelineStages[index].enabled;
        renderPipelineFlow();
        savePipelineStages();
    }
}

async function addPipelineStage(type) {
    if (!state.activePipelineId) {
        notify('Bitte zuerst eine Pipeline auswählen oder erstellen', 'warning');
        return;
    }

    const stageDefaults = {
        transcode: { params: { video_codec: 'libx264', preset: 'medium', crf: 23 } },
        scale: { params: { width: 1920, height: 1080 } },
        filter: { params: { brightness: 0, contrast: 1, saturation: 1 } },
        audio: { params: { codec: 'aac', bitrate: '128k', sample_rate: 44100 } },
        bitrate: { params: { video: '2M', audio: '128k' } },
        framerate: { params: { fps: 30 } },
        trim: { params: { start: '00:00:00', duration: '' } },
        deinterlace: { params: {} },
        denoise: { params: {} },
        stabilize: { params: {} },
    };

    const defaults = stageDefaults[type] || { params: {} };
    const stage = {
        type,
        label: type.charAt(0).toUpperCase() + type.slice(1),
        params: defaults.params,
        enabled: true,
    };

    state.pipelineStages.push(stage);
    renderPipelineFlow();
    await savePipelineStages();
    addLog(`Stufe hinzugefügt: ${type}`, 'info');
}

async function savePipelineStages() {
    if (!state.activePipelineId) return;

    try {
        await fetch(`/api/pipelines/${state.activePipelineId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ stages: state.pipelineStages }),
        });
    } catch (e) {
        console.error('Failed to save pipeline stages');
    }
}

async function deletePipeline(id) {
    if (!confirm('Pipeline löschen?')) return;
    await fetch(`/api/pipelines/${id}`, { method: 'DELETE' });
    if (state.activePipelineId === id) {
        state.activePipelineId = null;
        document.getElementById('pipelineEditor').style.display = 'none';
    }
    refreshPipelines();
}

async function runPipeline(id) {
    if (!state.uploadedFilePath) {
        notify('Bitte zuerst eine Datei hochladen (Konverter-Seite)', 'warning');
        return;
    }

    try {
        const resp = await fetch(`/api/pipelines/${id}/run`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                input_file: state.uploadedFilePath,
                output_format: state.selectedFormat,
            }),
        });
        const data = await resp.json();
        if (data.error) {
            notify(data.error, 'error');
            return;
        }
        addLog(`Pipeline ${id} ausgeführt, Job: ${data.id}`, 'success');
        notify('Pipeline gestartet', 'success');
        startJobPolling(data.id);
    } catch (e) {
        notify('Pipeline-Start fehlgeschlagen', 'error');
    }
}

// ============ QUEUE ============
async function refreshQueue() {
    try {
        const resp = await fetch('/api/queue');
        const data = await resp.json();
        renderQueue(data.queue || [], data.stats || {});
    } catch (e) {}
}

function renderQueue(queue, stats) {
    const el = document.getElementById('queueList');
    if (!queue.length) {
        el.innerHTML = '<div style="text-align:center; color:var(--text-dim); padding:20px;">Warteschlange ist leer</div>';
    } else {
        el.innerHTML = queue.map(job => `
            <div class="job-item">
                <div class="job-thumb">&#128196;</div>
                <div class="job-info">
                    <div class="job-name">${job.input_file || job.queue_id}</div>
                    <div class="job-meta">Priorität: ${job.priority || 5} | ${job.output_format || 'mp4'}</div>
                </div>
                <span class="job-status ${job.queue_status === 'waiting' ? 'queued' : job.queue_status}">${job.queue_status}</span>
            </div>
        `).join('');
    }

    document.getElementById('queueWaiting').textContent = stats.waiting || 0;
    document.getElementById('queueProcessing').textContent = stats.processing || 0;
    document.getElementById('queueCompleted').textContent = stats.completed || 0;
    document.getElementById('queueFailed').textContent = stats.failed || 0;
}

async function clearQueue() {
    await fetch('/api/queue', { method: 'DELETE' });
    refreshQueue();
    notify('Queue geleert', 'info');
}

// ============ STATUS / SYSTEM ============
async function refreshStatus() {
    try {
        const resp = await fetch('/api/system');
        const data = await resp.json();
        updateGauges(data);
        refreshJobs();
    } catch (e) {
        document.getElementById('systemStatusDot').className = 'status-dot error';
        document.getElementById('systemStatusText').textContent = 'OFFLINE';
    }
}

function updateGauges(data) {
    // CPU
    const cpuLoad = data.cpu_load?.[0] || 0;
    const cpuPercent = Math.min(100, cpuLoad * 25); // Normalize to ~100% at load 4
    document.getElementById('gaugeCpu').textContent = cpuLoad.toFixed(1);
    const cpuBar = document.getElementById('gaugeCpuBar');
    cpuBar.style.width = cpuPercent + '%';
    cpuBar.className = 'gauge-bar-fill' + (cpuPercent > 80 ? ' danger' : cpuPercent > 50 ? ' warning' : '');

    // Memory
    const mem = data.memory || {};
    const memPercent = mem.peak ? Math.round((mem.used / mem.peak) * 100) : 0;
    document.getElementById('gaugeMem').textContent = memPercent;
    const memBar = document.getElementById('gaugeMemBar');
    memBar.style.width = memPercent + '%';
    memBar.className = 'gauge-bar-fill' + (memPercent > 80 ? ' danger' : memPercent > 50 ? ' warning' : '');

    // Disk
    const diskFree = (data.disk?.free || 0) / (1024 * 1024 * 1024);
    const diskTotal = (data.disk?.total || 1) / (1024 * 1024 * 1024);
    const diskUsedPercent = Math.round(((diskTotal - diskFree) / diskTotal) * 100);
    document.getElementById('gaugeDisk').textContent = diskFree.toFixed(1);
    const diskBar = document.getElementById('gaugeDiskBar');
    diskBar.style.width = diskUsedPercent + '%';
    diskBar.className = 'gauge-bar-fill' + (diskUsedPercent > 90 ? ' danger' : diskUsedPercent > 70 ? ' warning' : '');

    // Status
    document.getElementById('systemStatusDot').className = 'status-dot';
    document.getElementById('systemStatusText').textContent = data.ffmpeg_available ? 'SYSTEM ONLINE' : 'FFMPEG MISSING';
    if (!data.ffmpeg_available) {
        document.getElementById('systemStatusDot').className = 'status-dot warning';
    }
}

function startAutoRefresh() {
    if (state.refreshInterval) clearInterval(state.refreshInterval);
    state.refreshInterval = setInterval(() => {
        if (state.currentPage === 'dashboard') refreshStatus();
        if (state.currentPage === 'streams') refreshStreams();
    }, 5000);
}

// ============ LOGGING ============
function addLog(message, level = 'info') {
    const console = document.getElementById('logConsole');
    const time = new Date().toLocaleTimeString();
    const line = document.createElement('div');
    line.className = `log-line ${level}`;
    line.innerHTML = `<span class="log-time">[${time}]</span> ${escapeHtml(message)}`;
    console.appendChild(line);
    console.scrollTop = console.scrollHeight;

    // Keep max 100 lines
    while (console.children.length > 100) {
        console.removeChild(console.firstChild);
    }
}

function clearLog() {
    document.getElementById('logConsole').innerHTML =
        '<div class="log-line info"><span class="log-time">[CLEAR]</span> Log bereinigt</div>';
}

// ============ NOTIFICATIONS ============
function notify(message, type = 'info') {
    const el = document.getElementById('notification');
    el.className = `notification ${type} show`;
    el.textContent = message;
    setTimeout(() => { el.classList.remove('show'); }, 3000);
}

// ============ HELPERS ============
function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    let i = 0, size = bytes;
    while (size >= 1024 && i < units.length - 1) { size /= 1024; i++; }
    return size.toFixed(1) + ' ' + units[i];
}

function formatDuration(seconds) {
    if (!seconds) return '0:00';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    return `${m}:${String(s).padStart(2, '0')}`;
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
