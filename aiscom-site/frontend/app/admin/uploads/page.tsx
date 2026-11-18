'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

interface Upload {
  id: number;
  filename: string;
  original_name: string;
  file_size: number;
  description: string;
  email: string;
  status: string;
  created_at: string;
}

export default function AdminUploadsPage() {
  const router = useRouter();
  const [uploads, setUploads] = useState<Upload[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    checkAuth();
    loadUploads();
  }, []);

  const checkAuth = async () => {
    const response = await fetch('/api/auth/check');
    const data = await response.json();
    if (!data.authenticated) {
      router.push('/admin');
    }
  };

  const loadUploads = async () => {
    try {
      const response = await fetch('/api/upload');
      if (response.ok) {
        const data = await response.json();
        setUploads(Array.isArray(data) ? data : []);
      }
    } catch (error) {
      console.error('Failed to load uploads:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const updateStatus = async (id: number, status: string) => {
    try {
      await fetch('/api/upload', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id, status }),
      });
      setUploads(uploads.map(u => u.id === id ? { ...u, status } : u));
    } catch (error) {
      console.error('Failed to update status:', error);
    }
  };

  const deleteUpload = async (id: number, filename: string) => {
    if (!confirm('Upload wirklich löschen?')) return;

    try {
      await fetch(`/api/upload?id=${id}&filename=${filename}`, { method: 'DELETE' });
      setUploads(uploads.filter(u => u.id !== id));
    } catch (error) {
      console.error('Failed to delete upload:', error);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('de-CH', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const statusColors: Record<string, string> = {
    new: 'bg-accent text-primary',
    processing: 'bg-yellow-500 text-black',
    completed: 'bg-green-500 text-white',
    rejected: 'bg-red-500 text-white',
  };

  const statusLabels: Record<string, string> = {
    new: 'Neu',
    processing: 'In Bearbeitung',
    completed: 'Abgeschlossen',
    rejected: 'Abgelehnt',
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-primary flex items-center justify-center">
        <div className="text-accent">Lädt...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-primary">
      {/* Header */}
      <header className="bg-primary-dark border-b border-primary-light/30 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/admin/dashboard" className="text-gray-400 hover:text-white transition-colors">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </Link>
            <h1 className="text-xl font-bold text-white">3D-Uploads</h1>
            <span className="px-2 py-1 bg-accent/20 text-accent text-sm rounded">
              {uploads.filter(u => u.status === 'new').length} neu
            </span>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {uploads.length === 0 ? (
          <Card className="text-center py-16">
            <svg className="w-16 h-16 text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
            <p className="text-gray-400">Keine Uploads vorhanden</p>
          </Card>
        ) : (
          <div className="space-y-4">
            {uploads.map((upload) => (
              <Card key={upload.id}>
                <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <svg className="w-8 h-8 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                      </svg>
                      <div>
                        <h3 className="font-semibold text-white">{upload.original_name}</h3>
                        <p className="text-gray-400 text-sm">
                          {formatFileSize(upload.file_size)} • {formatDate(upload.created_at)}
                        </p>
                      </div>
                    </div>

                    <div className="mb-2">
                      <a
                        href={`mailto:${upload.email}`}
                        className="text-accent hover:text-accent-light transition-colors text-sm"
                      >
                        {upload.email}
                      </a>
                    </div>

                    {upload.description && (
                      <p className="text-gray-400 text-sm bg-primary rounded p-3">
                        {upload.description}
                      </p>
                    )}
                  </div>

                  <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
                    <select
                      value={upload.status}
                      onChange={(e) => updateStatus(upload.id, e.target.value)}
                      className={`px-3 py-1 rounded text-sm font-medium ${statusColors[upload.status]} cursor-pointer`}
                    >
                      {Object.entries(statusLabels).map(([value, label]) => (
                        <option key={value} value={value} className="bg-primary text-white">
                          {label}
                        </option>
                      ))}
                    </select>

                    <div className="flex gap-2">
                      <a
                        href={`/uploads/${upload.filename}`}
                        download={upload.original_name}
                        className="px-3 py-1 bg-primary-light text-white rounded text-sm hover:bg-primary transition-colors"
                      >
                        Download
                      </a>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => deleteUpload(upload.id, upload.filename)}
                        className="text-red-400 hover:text-red-300"
                      >
                        Löschen
                      </Button>
                    </div>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
