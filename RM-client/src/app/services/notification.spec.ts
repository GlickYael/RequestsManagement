import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { NotificationService } from './notification';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should keep a notification on screen until it is dismissed', () => {
    service.success('saved');
    expect(service.notifications().map(n => n.message)).toEqual(['saved']);

    vi.advanceTimersByTime(60_000);
    expect(service.notifications().length).toBe(1);
  });

  it('should remove a notification on dismiss', () => {
    service.error('failed');
    const id = service.notifications()[0].id;

    service.dismiss(id);
    expect(service.notifications().length).toBe(0);
  });
});
