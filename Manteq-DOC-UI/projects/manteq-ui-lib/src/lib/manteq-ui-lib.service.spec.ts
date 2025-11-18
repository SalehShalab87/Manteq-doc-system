import { TestBed } from '@angular/core/testing';

import { ManteqUiLibService } from './manteq-ui-lib.service';

describe('ManteqUiLibService', () => {
  let service: ManteqUiLibService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ManteqUiLibService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
