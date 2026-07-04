import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    input: './openapi-spec.json',
    output: 'app/lib/admitto-api/generated',
    plugins: [
        {
            name: '@hey-api/client-fetch',
            runtimeConfigPath: '../admitto-client', // relative to /generated
        },
    ],
});