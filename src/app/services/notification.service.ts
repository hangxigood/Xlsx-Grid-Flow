/**
 * Notification Service - Toast notifications
 */

import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface Notification {
    id: string;
    type: NotificationType;
    message: string;
    duration: number;
}

@Injectable({
    providedIn: 'root',
})
export class NotificationService {
    private notifications = signal<Notification[]>([]);
    private nextId = 0;
    private recentMessages = new Map<string, number>(); // Track recent messages with timestamps
    private readonly DUPLICATE_THRESHOLD_MS = 1000; // 1 second window for duplicate detection

    readonly activeNotifications = this.notifications.asReadonly();

    /**
     * Show a success notification
     */
    success(message: string, duration: number = 3000): void {
        this.show('success', message, duration);
    }

    /**
     * Show an error notification
     */
    error(message: string, duration: number = 5000): void {
        this.show('error', message, duration);
    }

    /**
     * Show a warning notification
     */
    warning(message: string, duration: number = 4000): void {
        this.show('warning', message, duration);
    }

    /**
     * Show an info notification
     */
    info(message: string, duration: number = 3000): void {
        this.show('info', message, duration);
    }

    /**
     * Show a notification
     */
    private show(type: NotificationType, message: string, duration: number): void {
        // Check for duplicate messages within the threshold window
        const messageKey = `${type}:${message}`;
        const now = Date.now();
        const lastShown = this.recentMessages.get(messageKey);

        if (lastShown && (now - lastShown) < this.DUPLICATE_THRESHOLD_MS) {
            // Skip showing duplicate notification
            return;
        }

        // Update the timestamp for this message
        this.recentMessages.set(messageKey, now);

        // Clean up old entries from the map (older than threshold)
        for (const [key, timestamp] of this.recentMessages.entries()) {
            if (now - timestamp > this.DUPLICATE_THRESHOLD_MS) {
                this.recentMessages.delete(key);
            }
        }

        const id = `notification-${this.nextId++}`;
        const notification: Notification = { id, type, message, duration };

        this.notifications.update((notifications) => [...notifications, notification]);

        // Auto-dismiss after duration
        if (duration > 0) {
            setTimeout(() => {
                this.dismiss(id);
            }, duration);
        }
    }

    /**
     * Dismiss a notification by ID
     */
    dismiss(id: string): void {
        this.notifications.update((notifications) =>
            notifications.filter((n) => n.id !== id)
        );
    }

    /**
     * Clear all notifications
     */
    clearAll(): void {
        this.notifications.set([]);
    }
}
