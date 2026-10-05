import { TranslocoGlobalConfig } from '@jsverse/transloco-utils';

/** Configuración de @jsverse/transloco-keys-manager (`npx transloco-keys-manager extract|find`). */
const config: TranslocoGlobalConfig = {
  rootTranslationsPath: 'public/i18n/',
  langs: ['es', 'en'],
  keysManager: {
    input: ['src/app'],
    output: 'public/i18n',
    unflat: true,
    sort: true,
  },
};

export default config;
