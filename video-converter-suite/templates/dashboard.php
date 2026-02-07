<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= $config['app_name'] ?> - Control Panel</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Orbitron:wght@400;600;700;900&family=JetBrains+Mono:wght@300;400;500;600;700&family=Rajdhani:wght@400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="/css/controlpanel.css">
</head>
<body>

<!-- ====== TOP BAR ====== -->
<div class="topbar">
    <div class="topbar-logo">
        <div class="reactor-icon">&#9762;</div>
        <div>
            <div class="topbar-title">Video Converter Suite</div>
            <div class="topbar-subtitle">Pipeline Control System v<?= $config['version'] ?></div>
        </div>
    </div>
    <div class="topbar-status">
        <div class="status-indicator">
            <span class="status-dot" id="systemStatusDot"></span>
            <span id="systemStatusText">SYSTEM ONLINE</span>
        </div>
        <div class="status-indicator">
            <span>JOBS:</span>
            <span id="activeJobCount" style="color: var(--accent-cyan)">0</span>
        </div>
        <div class="clock" id="systemClock">00:00:00</div>
    </div>
</div>

<!-- ====== NAVIGATION ====== -->
<div class="nav-bar">
    <button class="nav-tab active" data-page="dashboard">Dashboard</button>
    <button class="nav-tab" data-page="converter">Konverter</button>
    <button class="nav-tab" data-page="streams">Live Streams</button>
    <button class="nav-tab" data-page="pipelines">Pipelines</button>
    <button class="nav-tab" data-page="queue">Warteschlange</button>
</div>

<!-- ====== PAGES ====== -->
<div class="main-container">

    <!-- ==================== DASHBOARD PAGE ==================== -->
    <div id="page-dashboard" class="page-content">
        <!-- System Gauges -->
        <div class="panel-row cols-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9881;</span> SYSTEM MONITOR</div>
                    <button class="btn btn-icon" onclick="refreshStatus()" data-tooltip="Refresh">&#8635;</button>
                </div>
                <div class="module-body">
                    <div class="gauge-grid">
                        <div class="gauge">
                            <div class="gauge-label">CPU Load</div>
                            <div class="gauge-value" id="gaugeCpu">0.0</div>
                            <div class="gauge-unit">Load Avg</div>
                            <div class="gauge-bar"><div class="gauge-bar-fill" id="gaugeCpuBar" style="width:0%"></div></div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Memory</div>
                            <div class="gauge-value" id="gaugeMem">0</div>
                            <div class="gauge-unit">% Used</div>
                            <div class="gauge-bar"><div class="gauge-bar-fill" id="gaugeMemBar" style="width:0%"></div></div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Disk</div>
                            <div class="gauge-value" id="gaugeDisk">0</div>
                            <div class="gauge-unit">GB Free</div>
                            <div class="gauge-bar"><div class="gauge-bar-fill" id="gaugeDiskBar" style="width:0%"></div></div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Active Jobs</div>
                            <div class="gauge-value" id="gaugeJobs">0</div>
                            <div class="gauge-unit">Running</div>
                            <div class="gauge-bar"><div class="gauge-bar-fill" id="gaugeJobsBar" style="width:0%"></div></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Active Jobs & Log -->
        <div class="panel-row cols-2-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9654;</span> AKTIVE JOBS</div>
                    <button class="btn" onclick="refreshJobs()">Aktualisieren</button>
                </div>
                <div class="module-body">
                    <div class="job-list" id="jobList">
                        <div style="text-align:center; color: var(--text-dim); padding: 20px;">
                            Keine aktiven Jobs
                        </div>
                    </div>
                </div>
            </div>
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9783;</span> SYSTEM LOG</div>
                    <button class="btn btn-icon" onclick="clearLog()">&#10005;</button>
                </div>
                <div class="module-body">
                    <div class="log-console" id="logConsole">
                        <div class="log-line info"><span class="log-time">[INIT]</span> Video Converter Suite gestartet</div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- ==================== CONVERTER PAGE ==================== -->
    <div id="page-converter" class="page-content" style="display:none">
        <div class="panel-row cols-2">
            <!-- Upload & Input -->
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#8682;</span> EINGANG / UPLOAD</div>
                </div>
                <div class="module-body">
                    <div class="upload-zone" id="uploadZone" onclick="document.getElementById('fileInput').click()">
                        <div class="upload-icon">&#128193;</div>
                        <div class="upload-text">Datei hierher ziehen oder klicken</div>
                        <div class="upload-hint">Alle Video- und Audio-Formate / max. 5 GB</div>
                        <input type="file" id="fileInput" accept="video/*,audio/*" style="display:none" onchange="handleFileSelect(event)">
                    </div>
                    <div id="uploadedFileInfo" style="display:none; margin-top:12px;"></div>
                </div>
            </div>

            <!-- Format Switchboard -->
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9881;</span> AUSGANGSFORMAT</div>
                </div>
                <div class="module-body">
                    <div class="form-label" style="margin-bottom:8px">Video-Formate</div>
                    <div class="format-matrix" id="videoFormatMatrix">
                        <?php foreach ($config['formats']['video'] as $key => $fmt): ?>
                        <div class="format-switch <?= $key === 'mp4' ? 'selected' : '' ?>" data-format="<?= $key ?>" onclick="selectFormat('<?= $key ?>')">
                            <div class="format-name"><?= strtoupper($key) ?></div>
                            <div class="format-desc"><?= $fmt['codec'] ?></div>
                        </div>
                        <?php endforeach; ?>
                    </div>
                    <div class="form-label" style="margin:12px 0 8px">Audio-Formate</div>
                    <div class="format-matrix" id="audioFormatMatrix">
                        <?php foreach ($config['formats']['audio'] as $key => $fmt): ?>
                        <div class="format-switch" data-format="<?= $key ?>" onclick="selectFormat('<?= $key ?>')">
                            <div class="format-name"><?= strtoupper($key) ?></div>
                            <div class="format-desc"><?= $fmt['codec'] ?></div>
                        </div>
                        <?php endforeach; ?>
                    </div>
                </div>
            </div>
        </div>

        <!-- Settings Row -->
        <div class="panel-row cols-3">
            <!-- Preset -->
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9733;</span> PRESET</div>
                </div>
                <div class="module-body">
                    <div class="switch-panel" id="presetPanel">
                        <?php foreach ($config['presets'] as $key => $p): ?>
                        <div class="switch-unit <?= $key === 'balanced' ? 'active' : '' ?>" data-preset="<?= $key ?>" onclick="selectPreset('<?= $key ?>')">
                            <div class="switch-led"></div>
                            <div class="switch-label"><?= ucfirst($key) ?></div>
                        </div>
                        <?php endforeach; ?>
                    </div>
                </div>
            </div>

            <!-- Resolution -->
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9634;</span> AUFLOESUNG</div>
                </div>
                <div class="module-body">
                    <div class="switch-panel" id="resolutionPanel">
                        <div class="switch-unit active" data-resolution="original" onclick="selectResolution('original')">
                            <div class="switch-led"></div>
                            <div class="switch-label">Original</div>
                        </div>
                        <?php foreach ($config['resolutions'] as $key => $res): ?>
                        <div class="switch-unit" data-resolution="<?= $key ?>" onclick="selectResolution('<?= $key ?>')">
                            <div class="switch-led"></div>
                            <div class="switch-label"><?= $key ?></div>
                        </div>
                        <?php endforeach; ?>
                    </div>
                </div>
            </div>

            <!-- Controls -->
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9655;</span> STEUERUNG</div>
                </div>
                <div class="module-body" style="display:flex; flex-direction:column; gap:12px; align-items:center; justify-content:center; min-height:140px;">
                    <button class="btn btn-primary btn-large" id="btnStartConvert" onclick="startConversion()" disabled>
                        &#9654; KONVERTIERUNG STARTEN
                    </button>
                    <button class="btn btn-emergency" id="btnStopAll" onclick="stopAllJobs()" style="display:none">
                        &#9632; NOTAUS - ALLE STOPPEN
                    </button>
                    <div id="conversionStatus" style="font-size:11px; color:var(--text-dim); text-align:center;"></div>
                </div>
            </div>
        </div>
    </div>

    <!-- ==================== STREAMS PAGE ==================== -->
    <div id="page-streams" class="page-content" style="display:none">
        <div class="panel-row cols-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#128225;</span> LIVE STREAM STEUERUNG</div>
                    <button class="btn btn-primary" onclick="openStreamModal()">+ Neuer Stream</button>
                </div>
                <div class="module-body">
                    <div class="stream-matrix" id="streamMatrix">
                        <div style="text-align:center; color:var(--text-dim); padding:40px; grid-column: 1/-1;">
                            Keine aktiven Streams. Klicke "Neuer Stream" um zu beginnen.
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Stream Format Switchboard -->
        <div class="panel-row cols-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9762;</span> FORMAT-UMSCHALTER (LIVE)</div>
                    <div style="font-size:10px; color:var(--accent-yellow)">&#9888; Format-Wechsel unterbricht den Stream kurz</div>
                </div>
                <div class="module-body">
                    <div style="display:grid; grid-template-columns: 200px 1fr; gap: 16px; align-items:start;">
                        <div>
                            <div class="form-label">Aktiver Stream</div>
                            <select class="form-select" id="activeStreamSelect" onchange="selectActiveStream(this.value)">
                                <option value="">-- Stream wählen --</option>
                            </select>
                        </div>
                        <div>
                            <div class="form-label">Zielformat wählen (klick = sofort umschalten)</div>
                            <div class="format-matrix" id="streamFormatSwitchboard">
                                <?php foreach ($config['formats']['video'] as $key => $fmt): ?>
                                <div class="format-switch" data-stream-format="<?= $key ?>" onclick="switchStreamFormat('<?= $key ?>')">
                                    <div class="format-name"><?= strtoupper($key) ?></div>
                                    <div class="format-desc"><?= $fmt['codec'] ?></div>
                                </div>
                                <?php endforeach; ?>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- ==================== PIPELINES PAGE ==================== -->
    <div id="page-pipelines" class="page-content" style="display:none">
        <div class="panel-row cols-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9776;</span> PIPELINE DESIGNER</div>
                    <div style="display:flex; gap:8px;">
                        <button class="btn btn-primary" onclick="createPipeline()">+ Neue Pipeline</button>
                    </div>
                </div>
                <div class="module-body">
                    <!-- Pipeline List -->
                    <div id="pipelineList" style="margin-bottom:16px;"></div>

                    <!-- Pipeline Editor -->
                    <div id="pipelineEditor" style="display:none;">
                        <div class="form-label" style="margin-bottom:8px;">PIPELINE FLOW - Drag & Drop zum Umordnen</div>
                        <div class="pipeline-canvas">
                            <div class="pipeline-flow" id="pipelineFlow">
                                <!-- Input node (always present) -->
                                <div class="pipeline-node active">
                                    <div class="node-type">Input</div>
                                    <div class="node-name">Source</div>
                                    <div class="node-status"></div>
                                </div>
                            </div>
                        </div>

                        <!-- Stage Palette -->
                        <div class="form-label" style="margin:12px 0 8px;">VERFUEGBARE STUFEN - Klick zum Hinzufügen</div>
                        <div class="switch-panel" id="stagePalette">
                            <div class="switch-unit" onclick="addPipelineStage('transcode')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Transcode</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('scale')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Scale</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('filter')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Filter</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('audio')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Audio</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('bitrate')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Bitrate</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('framerate')">
                                <div class="switch-led"></div>
                                <div class="switch-label">FPS</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('trim')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Trim</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('deinterlace')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Deinterlace</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('denoise')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Denoise</div>
                            </div>
                            <div class="switch-unit" onclick="addPipelineStage('stabilize')">
                                <div class="switch-led"></div>
                                <div class="switch-label">Stabilize</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- ==================== QUEUE PAGE ==================== -->
    <div id="page-queue" class="page-content" style="display:none">
        <div class="panel-row cols-3-1">
            <div class="module">
                <div class="module-header">
                    <div class="module-title"><span class="icon">&#9776;</span> WARTESCHLANGE</div>
                    <div style="display:flex; gap:8px;">
                        <button class="btn" onclick="refreshQueue()">Aktualisieren</button>
                        <button class="btn btn-danger" onclick="clearQueue()">Queue leeren</button>
                    </div>
                </div>
                <div class="module-body">
                    <div class="job-list" id="queueList">
                        <div style="text-align:center; color: var(--text-dim); padding: 20px;">
                            Warteschlange ist leer
                        </div>
                    </div>
                </div>
            </div>
            <div class="module">
                <div class="module-header">
                    <div class="module-title">STATISTIK</div>
                </div>
                <div class="module-body">
                    <div style="display:flex; flex-direction:column; gap:12px;">
                        <div class="gauge">
                            <div class="gauge-label">Wartend</div>
                            <div class="gauge-value" id="queueWaiting">0</div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Verarbeitet</div>
                            <div class="gauge-value" id="queueProcessing">0</div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Abgeschlossen</div>
                            <div class="gauge-value" id="queueCompleted" style="color:var(--accent-green)">0</div>
                        </div>
                        <div class="gauge">
                            <div class="gauge-label">Fehlgeschlagen</div>
                            <div class="gauge-value" id="queueFailed" style="color:var(--accent-red)">0</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- ====== STREAM MODAL ====== -->
<div class="modal-overlay" id="streamModal">
    <div class="modal">
        <div class="modal-header">
            <h3>Neuen Stream starten</h3>
            <button class="modal-close" onclick="closeStreamModal()">&times;</button>
        </div>
        <div class="modal-body">
            <div class="form-group">
                <label class="form-label">Stream-URL (RTMP, RTSP, HTTP, Datei)</label>
                <input class="form-input" type="text" id="streamInputUrl" placeholder="rtmp://server/live/stream oder /pfad/zur/datei.mp4">
            </div>
            <div class="form-group">
                <label class="form-label">Ausgangsformat</label>
                <select class="form-select" id="streamOutputFormat">
                    <?php foreach ($config['formats']['video'] as $key => $fmt): ?>
                    <option value="<?= $key ?>"><?= strtoupper($key) ?> (<?= $fmt['codec'] ?>)</option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="form-group">
                <label class="form-label">Aufloesung</label>
                <select class="form-select" id="streamResolution">
                    <option value="">Original</option>
                    <?php foreach ($config['resolutions'] as $key => $res): ?>
                    <option value="<?= $key ?>"><?= $res['label'] ?> (<?= $res['width'] ?>x<?= $res['height'] ?>)</option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="form-group">
                <label class="form-label">Preset</label>
                <select class="form-select" id="streamPreset">
                    <?php foreach ($config['presets'] as $key => $p): ?>
                    <option value="<?= $key ?>" <?= $key === 'fast' ? 'selected' : '' ?>><?= ucfirst($key) ?> (CRF <?= $p['crf'] ?>)</option>
                    <?php endforeach; ?>
                </select>
            </div>
        </div>
        <div class="modal-footer">
            <button class="btn" onclick="closeStreamModal()">Abbrechen</button>
            <button class="btn btn-primary" onclick="startNewStream()">&#9654; Stream starten</button>
        </div>
    </div>
</div>

<!-- ====== NOTIFICATION CONTAINER ====== -->
<div id="notification" class="notification"></div>

<script src="/js/controlpanel.js"></script>
</body>
</html>
