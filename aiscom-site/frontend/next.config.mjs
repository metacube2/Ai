/** @type {import('next').NextConfig} */
const nextConfig = {
  // Enable static exports for production
  output: 'standalone',

  // Image optimization
  images: {
    domains: ['localhost', 'aiscom.ch'],
    formats: ['image/avif', 'image/webp'],
  },

  // Redirect root to default locale
  async redirects() {
    return [
      {
        source: '/',
        destination: '/de',
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
