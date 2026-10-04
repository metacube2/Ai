import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'AISCOM - IT-Dienstleistungen & 3D-Druck',
  description: 'Professionelle IT-Dienstleistungen, 3D-Druck, C# & SAP ABAP Entwicklung in Zürich',
  keywords: ['3D-Druck', 'C#', 'SAP ABAP', 'Webentwicklung', 'Zürich', 'Schweiz'],
  authors: [{ name: 'Ingo Kohler' }],
  openGraph: {
    title: 'AISCOM - IT-Dienstleistungen & 3D-Druck',
    description: 'Professionelle IT-Dienstleistungen, 3D-Druck, C# & SAP ABAP Entwicklung in Zürich',
    url: 'https://aiscom.ch',
    siteName: 'AISCOM',
    type: 'website',
    locale: 'de_CH',
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html>
      <body className="font-sans">
        {children}
      </body>
    </html>
  );
}
