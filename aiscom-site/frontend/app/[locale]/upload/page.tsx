'use client';

import PageHeader from '@/components/visual/PageHeader';
import { useState, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Textarea from '@/components/ui/Textarea';

export default function UploadPage() {
  const params = useParams();
  const locale = params.locale as Locale;
  const dictionary = getDictionary(locale);

  const [file, setFile] = useState<File | null>(null);
  const [email, setEmail] = useState('');
  const [description, setDescription] = useState('');
  const [isDragging, setIsDragging] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile && isValidFile(droppedFile)) {
      setFile(droppedFile);
    }
  }, []);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile && isValidFile(selectedFile)) {
      setFile(selectedFile);
    }
  };

  const isValidFile = (file: File): boolean => {
    const validExtensions = ['.stl', '.obj'];
    const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'));
    const maxSize = 50 * 1024 * 1024; // 50 MB

    if (!validExtensions.includes(extension)) {
      alert(locale === 'de'
        ? 'Nur STL und OBJ Dateien erlaubt'
        : locale === 'it'
        ? 'Solo file STL e OBJ consentiti'
        : 'Only STL and OBJ files allowed');
      return false;
    }

    if (file.size > maxSize) {
      alert(locale === 'de'
        ? 'Datei zu gross (max. 50 MB)'
        : locale === 'it'
        ? 'File troppo grande (max. 50 MB)'
        : 'File too large (max. 50 MB)');
      return false;
    }

    return true;
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file || !email) return;

    setIsSubmitting(true);
    setStatus('idle');

    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('email', email);
      formData.append('description', description);

      const response = await fetch('/api/upload', {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        setStatus('success');
        setFile(null);
        setEmail('');
        setDescription('');
      } else {
        setStatus('error');
      }
    } catch {
      setStatus('error');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="py-20">
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <PageHeader title={dictionary.upload.title} subtitle={dictionary.upload.subtitle} />
        <p className="-mt-10 mb-12 text-center text-gray-400">{dictionary.upload.description}</p>

        {/* Status Messages */}
        {status === 'success' && (
          <Card className="mb-6 bg-green-900/20 border border-green-500/30">
            <div className="flex items-center gap-3 text-green-400">
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              <span>{dictionary.upload.success}</span>
            </div>
          </Card>
        )}

        {status === 'error' && (
          <Card className="mb-6 bg-red-900/20 border border-red-500/30">
            <div className="flex items-center gap-3 text-red-400">
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <span>{dictionary.upload.error}</span>
            </div>
          </Card>
        )}

        {/* Upload Form */}
        <Card>
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Dropzone */}
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                {dictionary.upload.fields.file}
                <span className="text-accent ml-1">*</span>
              </label>
              <div
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                className={`border-2 border-dashed rounded-xl p-8 text-center transition-colors duration-200 cursor-pointer ${
                  isDragging
                    ? 'border-accent bg-accent/10'
                    : file
                    ? 'border-green-500/50 bg-green-500/10'
                    : 'border-primary-light/50 hover:border-accent/50'
                }`}
                onClick={() => document.getElementById('file-input')?.click()}
              >
                <input
                  id="file-input"
                  type="file"
                  accept=".stl,.obj"
                  onChange={handleFileChange}
                  className="hidden"
                />

                {file ? (
                  <div className="flex flex-col items-center">
                    <svg className="w-12 h-12 text-green-500 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <p className="text-white font-medium">{file.name}</p>
                    <p className="text-gray-400 text-sm">{formatFileSize(file.size)}</p>
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        setFile(null);
                      }}
                      className="mt-2 text-red-400 text-sm hover:text-red-300"
                    >
                      {locale === 'de' ? 'Entfernen' : locale === 'it' ? 'Rimuovi' : 'Remove'}
                    </button>
                  </div>
                ) : (
                  <div className="flex flex-col items-center">
                    <svg className="w-12 h-12 text-gray-500 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                    </svg>
                    <p className="text-gray-300">{dictionary.upload.dropzone}</p>
                    <p className="text-gray-500 text-sm mt-2">{dictionary.upload.formats}</p>
                  </div>
                )}
              </div>
            </div>

            {/* Email */}
            <Input
              type="email"
              label={dictionary.upload.fields.email}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              required
            />

            {/* Description */}
            <Textarea
              label={dictionary.upload.fields.description}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={
                locale === 'de'
                  ? 'Material, Farbe, Anzahl, besondere Anforderungen...'
                  : locale === 'it'
                  ? 'Materiale, colore, quantità, requisiti speciali...'
                  : 'Material, color, quantity, special requirements...'
              }
              rows={4}
            />

            {/* Submit */}
            <Button
              type="submit"
              size="lg"
              className="w-full"
              isLoading={isSubmitting}
              disabled={!file || !email || isSubmitting}
            >
              {dictionary.upload.submit}
            </Button>
          </form>
        </Card>

        {/* Info Box */}
        <Card className="mt-8">
          <h3 className="text-lg font-semibold text-white mb-4">
            {locale === 'de' ? 'Hinweise' : locale === 'it' ? 'Note' : 'Notes'}
          </h3>
          <ul className="space-y-2 text-gray-400 text-sm">
            <li className="flex items-start gap-2">
              <svg className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              {locale === 'de'
                ? 'Ich prüfe jede Datei und melde mich innerhalb von 24 Stunden mit einem Angebot.'
                : locale === 'it'
                ? 'Controllo ogni file e ti contatterò entro 24 ore con un preventivo.'
                : 'I review every file and will get back to you within 24 hours with a quote.'}
            </li>
            <li className="flex items-start gap-2">
              <svg className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              {locale === 'de'
                ? 'Bei Fragen zum 3D-Modell nehme ich direkt Kontakt auf.'
                : locale === 'it'
                ? 'Per domande sul modello 3D ti contatterò direttamente.'
                : 'If there are questions about the 3D model, I will contact you directly.'}
            </li>
          </ul>
        </Card>
      </div>
    </div>
  );
}
