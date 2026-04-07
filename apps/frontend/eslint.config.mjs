import nextPlugin from '@next/eslint-plugin-next';
import tsParser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';

export default [
  {
    ignores: ['.next/**', 'node_modules/**', 'next-env.d.ts', 'src/generated/**'],
  },
  {
    files: ['**/*.ts', '**/*.tsx'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
        project: './tsconfig.json',
        ecmaFeatures: {
          jsx: true,
        },
      },
    },
    plugins: {
      '@typescript-eslint': tsPlugin,
      '@next/next': nextPlugin,
    },
    rules: {
      ...tsPlugin.configs.recommended.rules,
      ...nextPlugin.configs['core-web-vitals'].rules,
      '@typescript-eslint/no-explicit-any': 'error',
      'no-restricted-syntax': [
        'error',
        {
          selector: "FunctionDeclaration[id.name='realtimeTone']",
          message: 'Use getRealtimeTone from @/lib/realtime-status.',
        },
        {
          selector: "FunctionDeclaration[id.name='realtimeLabel']",
          message: 'Use getRealtimeLabel from @/lib/realtime-status.',
        },
      ],
    },
  },
];
