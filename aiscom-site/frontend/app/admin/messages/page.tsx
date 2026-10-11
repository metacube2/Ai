'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

interface Message {
  id: number;
  name: string;
  email: string;
  message: string;
  read: number;
  created_at: string;
}

export default function AdminMessagesPage() {
  const router = useRouter();
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedMessage, setSelectedMessage] = useState<Message | null>(null);

  useEffect(() => {
    checkAuth();
    loadMessages();
  }, []);

  const checkAuth = async () => {
    const response = await fetch('/api/auth/check');
    const data = await response.json();
    if (!data.authenticated) {
      router.push('/admin');
    }
  };

  const loadMessages = async () => {
    try {
      const response = await fetch('/api/contact');
      if (response.ok) {
        const data = await response.json();
        setMessages(Array.isArray(data) ? data : []);
      }
    } catch (error) {
      console.error('Failed to load messages:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const markAsRead = async (id: number) => {
    try {
      await fetch('/api/contact', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id, action: 'markRead' }),
      });
      setMessages(messages.map(m => m.id === id ? { ...m, read: 1 } : m));
    } catch (error) {
      console.error('Failed to mark as read:', error);
    }
  };

  const deleteMessage = async (id: number) => {
    if (!confirm('Nachricht wirklich löschen?')) return;

    try {
      await fetch(`/api/contact?id=${id}`, { method: 'DELETE' });
      setMessages(messages.filter(m => m.id !== id));
      if (selectedMessage?.id === id) setSelectedMessage(null);
    } catch (error) {
      console.error('Failed to delete message:', error);
    }
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
            <h1 className="text-xl font-bold text-white">Nachrichten</h1>
            <span className="px-2 py-1 bg-accent/20 text-accent text-sm rounded">
              {messages.filter(m => !m.read).length} ungelesen
            </span>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Message List */}
          <div className="lg:col-span-1 space-y-4">
            {messages.length === 0 ? (
              <Card className="text-center py-8">
                <p className="text-gray-400">Keine Nachrichten vorhanden</p>
              </Card>
            ) : (
              messages.map((message) => (
                <Card
                  key={message.id}
                  variant="hover"
                  className={`cursor-pointer ${
                    selectedMessage?.id === message.id ? 'border-accent' : ''
                  } ${!message.read ? 'border-l-4 border-l-accent' : ''}`}
                  onClick={() => {
                    setSelectedMessage(message);
                    if (!message.read) markAsRead(message.id);
                  }}
                >
                  <div className="flex items-start justify-between mb-2">
                    <h3 className={`font-semibold ${!message.read ? 'text-white' : 'text-gray-300'}`}>
                      {message.name}
                    </h3>
                    {!message.read && (
                      <span className="w-2 h-2 bg-accent rounded-full"></span>
                    )}
                  </div>
                  <p className="text-accent text-sm mb-2">{message.email}</p>
                  <p className="text-gray-400 text-sm line-clamp-2">{message.message}</p>
                  <p className="text-gray-500 text-xs mt-2">{formatDate(message.created_at)}</p>
                </Card>
              ))
            )}
          </div>

          {/* Message Detail */}
          <div className="lg:col-span-2">
            {selectedMessage ? (
              <Card>
                <div className="flex items-start justify-between mb-6">
                  <div>
                    <h2 className="text-xl font-bold text-white">{selectedMessage.name}</h2>
                    <a
                      href={`mailto:${selectedMessage.email}`}
                      className="text-accent hover:text-accent-light transition-colors"
                    >
                      {selectedMessage.email}
                    </a>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => window.location.href = `mailto:${selectedMessage.email}?subject=Re: AISCOM Kontaktanfrage`}
                    >
                      Antworten
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => deleteMessage(selectedMessage.id)}
                      className="text-red-400 hover:text-red-300"
                    >
                      Löschen
                    </Button>
                  </div>
                </div>

                <div className="bg-primary rounded-lg p-4 mb-4">
                  <p className="text-gray-300 whitespace-pre-wrap">{selectedMessage.message}</p>
                </div>

                <p className="text-gray-500 text-sm">
                  Empfangen am {formatDate(selectedMessage.created_at)}
                </p>
              </Card>
            ) : (
              <Card className="text-center py-16">
                <svg className="w-16 h-16 text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <p className="text-gray-400">Wählen Sie eine Nachricht aus</p>
              </Card>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
