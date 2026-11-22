'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Textarea from '@/components/ui/Textarea';

interface CmsContent {
  id: number;
  page: string;
  section: string;
  content_de: string;
  content_en: string;
  content_it: string;
  updated_at: string;
}

const defaultSections = [
  { page: 'home', section: 'hero_title', label: 'Startseite - Hero Titel' },
  { page: 'home', section: 'hero_description', label: 'Startseite - Hero Beschreibung' },
  { page: 'about', section: 'intro', label: 'Über mich - Einleitung' },
  { page: 'about', section: 'text', label: 'Über mich - Haupttext' },
  { page: 'services', section: '3dprint_description', label: '3D-Druck - Beschreibung' },
  { page: 'services', section: 'csharp_description', label: 'C# - Beschreibung' },
  { page: 'services', section: 'abap_description', label: 'SAP ABAP - Beschreibung' },
  { page: 'services', section: 'web_description', label: 'Webentwicklung - Beschreibung' },
];

export default function AdminCmsPage() {
  const router = useRouter();
  const [contents, setContents] = useState<CmsContent[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [editContent, setEditContent] = useState<Partial<CmsContent> & { page: string; section: string; label?: string } | null>(null);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  useEffect(() => {
    checkAuth();
    loadContents();
  }, []);

  const checkAuth = async () => {
    const response = await fetch('/api/auth/check');
    const data = await response.json();
    if (!data.authenticated) {
      router.push('/admin');
    }
  };

  const loadContents = async () => {
    try {
      const response = await fetch('/api/cms');
      if (response.ok) {
        const data = await response.json();
        setContents(Array.isArray(data) ? data : []);
      }
    } catch (error) {
      console.error('Failed to load CMS content:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const saveContent = async () => {
    if (!editContent) return;

    setSaveStatus('saving');
    try {
      const response = await fetch('/api/cms', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(editContent),
      });

      if (response.ok) {
        setSaveStatus('saved');
        loadContents();
        setTimeout(() => setSaveStatus('idle'), 2000);
      } else {
        setSaveStatus('error');
      }
    } catch (error) {
      console.error('Failed to save content:', error);
      setSaveStatus('error');
    }
  };

  const getExistingContent = (page: string, section: string): CmsContent | undefined => {
    return contents.find(c => c.page === page && c.section === section);
  };

  const openEditor = (page: string, section: string, label: string) => {
    const existing = getExistingContent(page, section);
    setEditContent({
      page,
      section,
      label,
      content_de: existing?.content_de || '',
      content_en: existing?.content_en || '',
      content_it: existing?.content_it || '',
    });
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
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center gap-4">
          <Link href="/admin/dashboard" className="text-gray-400 hover:text-white transition-colors">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
          </Link>
          <h1 className="text-xl font-bold text-white">CMS - Texte bearbeiten</h1>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Edit Modal */}
        {editContent && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <Card className="w-full max-w-3xl max-h-[90vh] overflow-y-auto">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-bold text-white">{editContent.label}</h2>
                <button
                  onClick={() => setEditContent(null)}
                  className="text-gray-400 hover:text-white"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="space-y-6">
                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <span className="px-2 py-1 bg-red-500/20 text-red-400 text-xs rounded font-medium">DE</span>
                    <span className="text-sm text-gray-400">Deutsch</span>
                  </div>
                  <Textarea
                    value={editContent.content_de || ''}
                    onChange={(e) => setEditContent({ ...editContent, content_de: e.target.value })}
                    rows={4}
                    placeholder="Deutscher Text..."
                  />
                </div>

                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <span className="px-2 py-1 bg-blue-500/20 text-blue-400 text-xs rounded font-medium">EN</span>
                    <span className="text-sm text-gray-400">English</span>
                  </div>
                  <Textarea
                    value={editContent.content_en || ''}
                    onChange={(e) => setEditContent({ ...editContent, content_en: e.target.value })}
                    rows={4}
                    placeholder="English text..."
                  />
                </div>

                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <span className="px-2 py-1 bg-green-500/20 text-green-400 text-xs rounded font-medium">IT</span>
                    <span className="text-sm text-gray-400">Italiano</span>
                  </div>
                  <Textarea
                    value={editContent.content_it || ''}
                    onChange={(e) => setEditContent({ ...editContent, content_it: e.target.value })}
                    rows={4}
                    placeholder="Testo italiano..."
                  />
                </div>

                <div className="flex gap-4 pt-4">
                  <Button
                    onClick={saveContent}
                    className="flex-1"
                    isLoading={saveStatus === 'saving'}
                  >
                    {saveStatus === 'saved' ? 'Gespeichert!' : 'Speichern'}
                  </Button>
                  <Button variant="secondary" onClick={() => setEditContent(null)}>
                    Abbrechen
                  </Button>
                </div>

                {saveStatus === 'error' && (
                  <p className="text-red-400 text-sm text-center">
                    Fehler beim Speichern. Bitte versuchen Sie es erneut.
                  </p>
                )}
              </div>
            </Card>
          </div>
        )}

        {/* Content Sections */}
        <div className="space-y-8">
          {/* Group by page */}
          {['home', 'about', 'services'].map((page) => {
            const pageSections = defaultSections.filter(s => s.page === page);
            const pageLabel = page === 'home' ? 'Startseite' : page === 'about' ? 'Über mich' : 'Dienstleistungen';

            return (
              <div key={page}>
                <h2 className="text-lg font-semibold text-white mb-4">{pageLabel}</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {pageSections.map((section) => {
                    const existing = getExistingContent(section.page, section.section);
                    return (
                      <Card
                        key={`${section.page}-${section.section}`}
                        variant="hover"
                        className="cursor-pointer"
                        onClick={() => openEditor(section.page, section.section, section.label)}
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <h3 className="font-medium text-white mb-2">{section.label}</h3>
                            {existing ? (
                              <p className="text-gray-400 text-sm line-clamp-2">
                                {existing.content_de || 'Kein Text vorhanden'}
                              </p>
                            ) : (
                              <p className="text-gray-500 text-sm italic">Noch nicht bearbeitet</p>
                            )}
                          </div>
                          <svg className="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                          </svg>
                        </div>
                      </Card>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        {/* Info */}
        <Card className="mt-8 bg-accent/10 border border-accent/30">
          <div className="flex items-start gap-4">
            <svg className="w-6 h-6 text-accent flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <div>
              <h3 className="font-medium text-white mb-1">Hinweis</h3>
              <p className="text-gray-400 text-sm">
                Änderungen werden sofort gespeichert, aber erst nach einem Neubau der Website auf der Live-Seite sichtbar.
                Die grundlegenden Texte werden aus den Übersetzungsdateien geladen.
                Mit dem CMS können Sie spezifische Inhalte überschreiben.
              </p>
            </div>
          </div>
        </Card>
      </main>
    </div>
  );
}
