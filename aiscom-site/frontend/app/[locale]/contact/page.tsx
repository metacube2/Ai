'use client';

import PageHeader from '@/components/visual/PageHeader';
import { useState } from 'react';
import { useParams } from 'next/navigation';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Textarea from '@/components/ui/Textarea';

export default function ContactPage() {
  const params = useParams();
  const locale = params.locale as Locale;
  const dictionary = getDictionary(locale);

  const [formData, setFormData] = useState({
    name: '',
    email: '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setStatus('idle');

    try {
      const response = await fetch('/api/contact', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        setStatus('success');
        setFormData({ name: '', email: '', message: '' });
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
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <PageHeader title={dictionary.contact.title} subtitle={dictionary.contact.subtitle} />
        <p className="-mt-10 mb-12 text-center text-gray-400">{dictionary.contact.description}</p>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
          {/* Contact Form */}
          <Card>
            {/* Status Messages */}
            {status === 'success' && (
              <div className="mb-6 p-4 bg-green-900/20 border border-green-500/30 rounded-lg">
                <div className="flex items-center gap-3 text-green-400">
                  <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  <span>{dictionary.contact.success}</span>
                </div>
              </div>
            )}

            {status === 'error' && (
              <div className="mb-6 p-4 bg-red-900/20 border border-red-500/30 rounded-lg">
                <div className="flex items-center gap-3 text-red-400">
                  <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                  <span>{dictionary.contact.error}</span>
                </div>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
              <Input
                name="name"
                label={dictionary.contact.fields.name}
                value={formData.name}
                onChange={handleChange}
                placeholder={locale === 'de' ? 'Max Mustermann' : locale === 'it' ? 'Mario Rossi' : 'John Doe'}
                required
              />

              <Input
                type="email"
                name="email"
                label={dictionary.contact.fields.email}
                value={formData.email}
                onChange={handleChange}
                placeholder="name@example.com"
                required
              />

              <Textarea
                name="message"
                label={dictionary.contact.fields.message}
                value={formData.message}
                onChange={handleChange}
                placeholder={
                  locale === 'de'
                    ? 'Beschreiben Sie Ihr Projekt oder Ihre Anfrage...'
                    : locale === 'it'
                    ? 'Descrivi il tuo progetto o la tua richiesta...'
                    : 'Describe your project or inquiry...'
                }
                rows={5}
                required
              />

              <Button
                type="submit"
                size="lg"
                className="w-full"
                isLoading={isSubmitting}
                disabled={isSubmitting}
              >
                {dictionary.contact.submit}
              </Button>
            </form>
          </Card>

          {/* Contact Info */}
          <div className="space-y-6">
            <Card>
              <h3 className="text-xl font-semibold text-white mb-4">{dictionary.contact.direct}</h3>
              <a
                href="mailto:ingo.kohler.zh@gmail.com"
                className="flex items-center gap-3 text-accent hover:text-accent-light transition-colors text-lg"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                ingo.kohler.zh@gmail.com
              </a>
            </Card>

            <Card>
              <h3 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Standort' : locale === 'it' ? 'Posizione' : 'Location'}
              </h3>
              <div className="flex items-start gap-3 text-gray-300">
                <svg className="w-6 h-6 text-accent flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <div>
                  <p>8052 Zürich</p>
                  <p className="text-gray-500">
                    {locale === 'de' ? 'Schweiz' : locale === 'it' ? 'Svizzera' : 'Switzerland'}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <h3 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Antwortzeit' : locale === 'it' ? 'Tempo di risposta' : 'Response Time'}
              </h3>
              <div className="flex items-start gap-3 text-gray-300">
                <svg className="w-6 h-6 text-accent flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p>
                  {locale === 'de'
                    ? 'In der Regel innerhalb von 24 Stunden'
                    : locale === 'it'
                    ? 'Di solito entro 24 ore'
                    : 'Usually within 24 hours'}
                </p>
              </div>
            </Card>

            <Card className="bg-gradient-to-br from-accent/10 to-primary-light border border-accent/30">
              <h3 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? '3D-Druck Anfrage?' : locale === 'it' ? 'Richiesta stampa 3D?' : '3D Print Request?'}
              </h3>
              <p className="text-gray-300 mb-4">
                {locale === 'de'
                  ? 'Für 3D-Druck Anfragen nutzen Sie bitte den Upload-Bereich.'
                  : locale === 'it'
                  ? 'Per richieste di stampa 3D, utilizzare la sezione upload.'
                  : 'For 3D print requests, please use the upload section.'}
              </p>
              <a
                href={`/${locale}/upload`}
                className="inline-flex items-center gap-2 text-accent hover:text-accent-light transition-colors font-medium"
              >
                {locale === 'de' ? 'Zum Upload' : locale === 'it' ? 'Vai all\'upload' : 'Go to Upload'}
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </a>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
