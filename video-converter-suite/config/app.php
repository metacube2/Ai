<?php

return [
    'app_name' => 'Video Converter Suite',
    'version' => '1.0.0',
    'debug' => true,

    'ffmpeg' => [
        'binary' => getenv('FFMPEG_PATH') ?: '/usr/bin/ffmpeg',
        'ffprobe' => getenv('FFPROBE_PATH') ?: '/usr/bin/ffprobe',
        'threads' => (int)(getenv('FFMPEG_THREADS') ?: 4),
        'timeout' => 3600,
        'nice' => 10,
    ],

    'storage' => [
        'uploads' => __DIR__ . '/../storage/uploads',
        'outputs' => __DIR__ . '/../storage/outputs',
        'thumbnails' => __DIR__ . '/../storage/thumbnails',
        'logs' => __DIR__ . '/../storage/logs',
        'temp' => __DIR__ . '/../storage/temp',
    ],

    'limits' => [
        'max_upload_size' => 5 * 1024 * 1024 * 1024, // 5 GB
        'max_concurrent_jobs' => 3,
        'max_pipeline_depth' => 10,
    ],

    'websocket' => [
        'host' => '0.0.0.0',
        'port' => 8081,
    ],

    'formats' => [
        'video' => [
            'mp4'  => ['codec' => 'libx264',  'ext' => 'mp4',  'mime' => 'video/mp4'],
            'webm' => ['codec' => 'libvpx-vp9', 'ext' => 'webm', 'mime' => 'video/webm'],
            'mkv'  => ['codec' => 'libx264',  'ext' => 'mkv',  'mime' => 'video/x-matroska'],
            'avi'  => ['codec' => 'mpeg4',    'ext' => 'avi',  'mime' => 'video/x-msvideo'],
            'mov'  => ['codec' => 'libx264',  'ext' => 'mov',  'mime' => 'video/quicktime'],
            'flv'  => ['codec' => 'flv1',     'ext' => 'flv',  'mime' => 'video/x-flv'],
            'wmv'  => ['codec' => 'wmv2',     'ext' => 'wmv',  'mime' => 'video/x-ms-wmv'],
            'ts'   => ['codec' => 'libx264',  'ext' => 'ts',   'mime' => 'video/mp2t'],
            'hls'  => ['codec' => 'libx264',  'ext' => 'm3u8', 'mime' => 'application/x-mpegURL'],
            'dash' => ['codec' => 'libx264',  'ext' => 'mpd',  'mime' => 'application/dash+xml'],
        ],
        'audio' => [
            'aac'  => ['codec' => 'aac',         'ext' => 'aac',  'mime' => 'audio/aac'],
            'mp3'  => ['codec' => 'libmp3lame',   'ext' => 'mp3',  'mime' => 'audio/mpeg'],
            'ogg'  => ['codec' => 'libvorbis',    'ext' => 'ogg',  'mime' => 'audio/ogg'],
            'wav'  => ['codec' => 'pcm_s16le',    'ext' => 'wav',  'mime' => 'audio/wav'],
            'flac' => ['codec' => 'flac',         'ext' => 'flac', 'mime' => 'audio/flac'],
            'opus' => ['codec' => 'libopus',      'ext' => 'opus', 'mime' => 'audio/opus'],
        ],
    ],

    'presets' => [
        'ultrafast' => ['preset' => 'ultrafast', 'crf' => 28],
        'fast'      => ['preset' => 'fast',      'crf' => 23],
        'balanced'  => ['preset' => 'medium',    'crf' => 20],
        'quality'   => ['preset' => 'slow',      'crf' => 18],
        'lossless'  => ['preset' => 'veryslow',  'crf' => 0],
    ],

    'resolutions' => [
        '4k'    => ['width' => 3840, 'height' => 2160, 'label' => '4K UHD'],
        '1440p' => ['width' => 2560, 'height' => 1440, 'label' => '2K QHD'],
        '1080p' => ['width' => 1920, 'height' => 1080, 'label' => 'Full HD'],
        '720p'  => ['width' => 1280, 'height' => 720,  'label' => 'HD'],
        '480p'  => ['width' => 854,  'height' => 480,  'label' => 'SD'],
        '360p'  => ['width' => 640,  'height' => 360,  'label' => 'Low'],
    ],
];
