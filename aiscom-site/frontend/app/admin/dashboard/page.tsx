'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

interface Stats {
  messages: number;
  unreadMessages: number;
  uploads: number;
  newUploads: number;
  portfolioItems: number;
}

export default function AdminDashboard() {
  const router = useRouter();
  const [stats, setStats] = useState<Stats>({
    messages: 0,
    unreadMessages: 0,
    uploads: 0,
    newUploads: 0,
    portfolioItems: 0,
  });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    checkAuth();
    loadStats();
  }, []);

  const checkAuth = async () => {
    const response = await fetch('/api/auth/check');
    const data = await response.json();
    if (!data.authenticated) {
      router.push('/admin');
    }
  };

  const loadStats = async () => {
    try {
      const [messagesRes, uploadsRes, portfolioRes] = await Promise.all([
        fetch('/api/contact'),
        fetch('/api/upload'),
        fetch('/api/portfolio?all=true'),
      ]);

      const messages = messagesRes.ok ? await messagesRes.json() : [];
      const uploads = uploadsRes.ok ? await uploadsRes.json() : [];
      const portfolio = portfolioRes.ok ? await portfolioRes.json() : [];

      setStats({
        messages: Array.isArray(messages) ? messages.length : 0,
        unreadMessages: Array.isArray(messages) ? messages.filter((m: { read: number }) => !m.read).length : 0,
        uploads: Array.isArray(uploads) ? uploads.length : 0,
        newUploads: Array.isArray(uploads) ? uploads.filter((u: { status: string }) => u.status === 'new').length : 0,
        portfolioItems: Array.isArray(portfolio) ? portfolio.length : 0,
      });
    } catch (error) {
      console.error('Failed to load stats:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = async () => {
    await fetch('/api/auth/logout', { method: 'POST' });
    router.push('/admin');
  };

  const dashboardItems = [
    {
      title: 'Nachrichten',
      description: 'Kontaktanfragen verwalten',
      href: '/admin/messages',
      icon: (
        <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
      ),
      stat: stats.messages,
      badge: stats.unreadMessages,
    },
    {
      title: '3D-Uploads',
      description: 'Hochgeladene Dateien verwalten',
      href: '/admin/uploads',
      icon: (
        <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
        </svg>
      ),
      stat: stats.uploads,
      badge: stats.newUploads,
    },
    {
      title: 'Portfolio',
      description: 'Projekte hinzufügen und bearbeiten',
      href: '/admin/portfolio',
      icon: (
        <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
        </svg>
      ),
      stat: stats.portfolioItems,
    },
    {
      title: 'CMS',
      description: 'Website-Texte bearbeiten',
      href: '/admin/cms',
      icon: (
        <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
      ),
    },
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
            <h1 className="text-xl font-bold text-white">AISCOM Admin</h1>
            <span className="text-gray-500">|</span>
            <span className="text-gray-400">Dashboard</span>
          </div>
          <div className="flex items-center gap-4">
            <a href="/de" target="_blank" className="text-gray-400 hover:text-accent text-sm transition-colors">
              Website ansehen
            </a>
            <Button variant="ghost" size="sm" onClick={handleLogout}>
              Abmelden
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h2 className="text-2xl font-bold text-white mb-2">Willkommen zurück!</h2>
          <p className="text-gray-400">Hier ist eine Übersicht Ihrer Website.</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {dashboardItems.map((item) => (
            <Link key={item.href} href={item.href}>
              <Card variant="hover" className="h-full">
                <div className="flex items-start justify-between mb-4">
                  <div className="text-accent">{item.icon}</div>
                  {item.badge !== undefined && item.badge > 0 && (
                    <span className="px-2 py-1 bg-accent text-primary text-xs font-bold rounded-full">
                      {item.badge} neu
                    </span>
                  )}
                </div>
                <h3 className="text-lg font-semibold text-white mb-1">{item.title}</h3>
                <p className="text-gray-400 text-sm mb-3">{item.description}</p>
                {item.stat !== undefined && (
                  <p className="text-2xl font-bold text-accent">{item.stat}</p>
                )}
              </Card>
            </Link>
          ))}
        </div>

        {/* Quick Stats */}
        <Card className="mt-8">
          <h3 className="text-lg font-semibold text-white mb-4">Schnellübersicht</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="p-4 bg-primary rounded-lg text-center">
              <div className="text-3xl font-bold text-accent">{stats.unreadMessages}</div>
              <div className="text-gray-400 text-sm">Ungelesene Nachrichten</div>
            </div>
            <div className="p-4 bg-primary rounded-lg text-center">
              <div className="text-3xl font-bold text-accent">{stats.newUploads}</div>
              <div className="text-gray-400 text-sm">Neue Uploads</div>
            </div>
            <div className="p-4 bg-primary rounded-lg text-center">
              <div className="text-3xl font-bold text-accent">{stats.portfolioItems}</div>
              <div className="text-gray-400 text-sm">Portfolio-Einträge</div>
            </div>
            <div className="p-4 bg-primary rounded-lg text-center">
              <div className="text-3xl font-bold text-accent">{stats.messages + stats.uploads}</div>
              <div className="text-gray-400 text-sm">Gesamt Anfragen</div>
            </div>
          </div>
        </Card>
      </main>
    </div>
  );
}
