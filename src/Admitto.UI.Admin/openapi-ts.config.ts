import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    input: 'http://localhost:5100/openapi/v1.json',
    output: 'app/api-client',
    plugins: ['@hey-api/client-fetch'],
});