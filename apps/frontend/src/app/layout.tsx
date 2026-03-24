import type { Metadata } from 'next';
import { IBM_Plex_Sans, Space_Grotesk } from 'next/font/google';
import { AppProviders } from '@/components/providers/app-providers';
import '@/app/globals.css';

const bodyFont = IBM_Plex_Sans({
  subsets: ['latin'],
  variable: '--font-body',
  weight: ['400', '500', '600'],
});

const displayFont = Space_Grotesk({
  subsets: ['latin'],
  variable: '--font-display',
  weight: ['500', '700'],
});

export const metadata: Metadata = {
  title: 'RLApp Frontend Console',
  description: 'Operational frontend aligned to the implemented RLApp backend contract.',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${bodyFont.variable} ${displayFont.variable}`}>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
