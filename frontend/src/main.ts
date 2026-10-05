import { registerLocaleData } from '@angular/common';
import localeEsCl from '@angular/common/locales/es-CL';
import localeEsBo from '@angular/common/locales/es-BO';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app';

registerLocaleData(localeEsCl, 'es-CL');
registerLocaleData(localeEsBo, 'es-BO');

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
