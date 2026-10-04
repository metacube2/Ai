'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Textarea from '@/components/ui/Textarea';

interface PortfolioItem {
  id: number;
  title_de: string;
  title_en: string;
  title_it: string;
  description_de: string;
  description_en: string;
  description_it: string;
  category: string;
  image_path: string;
  active: number;
  sort_order: number;
  created_at: string;
}

const emptyItem: Omit<PortfolioItem, 'id' | 'created_at'> = {
  title_de: '',
  title_en: '',
  title_it: '',
  description_de: '',
  description_en: '',
  description_it: '',
  category: '3dprint',
  image_path: '',
  active: 1,
  sort_order: 0,
};

export default function AdminPortfolioPage() {
  const router = useRouter();
  const [items, setItems] = useState<PortfolioItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [editItem, setEditItem] = useState<Partial<PortfolioItem> | null>(null);
  const [isNew, setIsNew] = useState(false);

  useEffect(() => {
    checkAuth();
    loadItems();
  }, []);

  const checkAuth = async () => {
    const response = await fetch('/api/auth/check');
    const data = await response.json();
    if (!data.authenticated) {
      router.push('/admin');
    }
  };

  const loadItems = async () => {
    try {
      const response = await fetch('/api/portfolio?all=true');
      if (response.ok) {
        const data = await response.json();
        setItems(Array.isArray(data) ? data : []);
      }
    } catch (error) {
      console.error('Failed to load portfolio:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const saveItem = async () => {
    if (!editItem) return;

    try {
      const method = isNew ? 'POST' : 'PATCH';
      const response = await fetch('/api/portfolio', {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(editItem),
      });

      if (response.ok) {
        loadItems();
        setEditItem(null);
        setIsNew(false);
      }
    } catch (error) {
      console.error('Failed to save item:', error);
    }
  };

  const deleteItem = async (id: number) => {
    if (!confirm('Eintrag wirklich löschen?')) return;

    try {
      await fetch(`/api/portfolio?id=${id}`, { method: 'DELETE' });
      setItems(items.filter(i => i.id !== id));
    } catch (error) {
      console.error('Failed to delete item:', error);
    }
  };

  const categories = [
    { value: '3dprint', label: '3D-Druck' },
    { value: 'software', label: 'Software' },
    { value: 'web', label: 'Web' },
  ];

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
            <h1 className="text-xl font-bold text-white">Portfolio</h1>
          </div>
          <Button
            onClick={() => {
              setEditItem(emptyItem);
              setIsNew(true);
            }}
          >
            Neuer Eintrag
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Edit Modal */}
        {editItem && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <Card className="w-full max-w-2xl max-h-[90vh] overflow-y-auto">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-bold text-white">
                  {isNew ? 'Neuer Eintrag' : 'Eintrag bearbeiten'}
                </h2>
                <button
                  onClick={() => {
                    setEditItem(null);
                    setIsNew(false);
                  }}
                  className="text-gray-400 hover:text-white"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="space-y-6">
                {/* Titles */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Input
                    label="Titel (DE)"
                    value={editItem.title_de || ''}
                    onChange={(e) => setEditItem({ ...editItem, title_de: e.target.value })}
                    required
                  />
                  <Input
                    label="Titel (EN)"
                    value={editItem.title_en || ''}
                    onChange={(e) => setEditItem({ ...editItem, title_en: e.target.value })}
                  />
                  <Input
                    label="Titel (IT)"
                    value={editItem.title_it || ''}
                    onChange={(e) => setEditItem({ ...editItem, title_it: e.target.value })}
                  />
                </div>

                {/* Descriptions */}
                <Textarea
                  label="Beschreibung (DE)"
                  value={editItem.description_de || ''}
                  onChange={(e) => setEditItem({ ...editItem, description_de: e.target.value })}
                  rows={3}
                />
                <Textarea
                  label="Beschreibung (EN)"
                  value={editItem.description_en || ''}
                  onChange={(e) => setEditItem({ ...editItem, description_en: e.target.value })}
                  rows={3}
                />
                <Textarea
                  label="Beschreibung (IT)"
                  value={editItem.description_it || ''}
                  onChange={(e) => setEditItem({ ...editItem, description_it: e.target.value })}
                  rows={3}
                />

                {/* Category */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">Kategorie</label>
                  <select
                    value={editItem.category || '3dprint'}
                    onChange={(e) => setEditItem({ ...editItem, category: e.target.value })}
                    className="w-full px-4 py-3 bg-primary border border-primary-light/50 rounded-lg text-white"
                  >
                    {categories.map((cat) => (
                      <option key={cat.value} value={cat.value}>
                        {cat.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Image Path */}
                <Input
                  label="Bild-Pfad"
                  value={editItem.image_path || ''}
                  onChange={(e) => setEditItem({ ...editItem, image_path: e.target.value })}
                  placeholder="/uploads/image.jpg"
                />

                {/* Sort Order & Active */}
                <div className="grid grid-cols-2 gap-4">
                  <Input
                    type="number"
                    label="Sortierung"
                    value={editItem.sort_order || 0}
                    onChange={(e) => setEditItem({ ...editItem, sort_order: parseInt(e.target.value) })}
                  />
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">Status</label>
                    <select
                      value={editItem.active || 1}
                      onChange={(e) => setEditItem({ ...editItem, active: parseInt(e.target.value) })}
                      className="w-full px-4 py-3 bg-primary border border-primary-light/50 rounded-lg text-white"
                    >
                      <option value={1}>Aktiv</option>
                      <option value={0}>Inaktiv</option>
                    </select>
                  </div>
                </div>

                {/* Actions */}
                <div className="flex gap-4 pt-4">
                  <Button onClick={saveItem} className="flex-1">
                    Speichern
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => {
                      setEditItem(null);
                      setIsNew(false);
                    }}
                  >
                    Abbrechen
                  </Button>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* Items List */}
        {items.length === 0 ? (
          <Card className="text-center py-16">
            <svg className="w-16 h-16 text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
            </svg>
            <p className="text-gray-400 mb-4">Keine Portfolio-Einträge vorhanden</p>
            <Button
              onClick={() => {
                setEditItem(emptyItem);
                setIsNew(true);
              }}
            >
              Ersten Eintrag erstellen
            </Button>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {items.map((item) => (
              <Card key={item.id} className={`${!item.active ? 'opacity-50' : ''}`}>
                <div className="aspect-video bg-primary-dark rounded-lg mb-4 flex items-center justify-center">
                  {item.image_path ? (
                    <img
                      src={item.image_path}
                      alt={item.title_de}
                      className="w-full h-full object-cover rounded-lg"
                    />
                  ) : (
                    <svg className="w-12 h-12 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                  )}
                </div>

                <div className="flex items-start justify-between mb-2">
                  <h3 className="font-semibold text-white">{item.title_de}</h3>
                  <span className="px-2 py-1 bg-accent/20 text-accent text-xs rounded">
                    {categories.find(c => c.value === item.category)?.label || item.category}
                  </span>
                </div>

                <p className="text-gray-400 text-sm mb-4 line-clamp-2">{item.description_de}</p>

                <div className="flex gap-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setEditItem(item);
                      setIsNew(false);
                    }}
                    className="flex-1"
                  >
                    Bearbeiten
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => deleteItem(item.id)}
                    className="text-red-400 hover:text-red-300"
                  >
                    Löschen
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
