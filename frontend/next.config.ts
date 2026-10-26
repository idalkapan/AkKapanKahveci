import type { NextConfig } from 'next';
const config: NextConfig = { async rewrites() { return [{ source: '/api/:path*', destination: `${process.env.API_URL || 'http://localhost:5050'}/api/:path*` }]; } };
export default config;
