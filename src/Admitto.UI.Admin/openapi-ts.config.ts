import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    input: 'http://localhost:5100/openapi/v1.json',
    output: 'app/lib/admitto-api/generated',
    plugins: [
        {
            name: '@hey-api/client-fetch',
            runtimeConfigPath: '../admitto-client', // relative to /generated
        },
    ],
});