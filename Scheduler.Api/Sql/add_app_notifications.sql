USE scheduler_db;

CREATE TABLE IF NOT EXISTS app_notifications (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id BIGINT UNSIGNED NOT NULL,
    appointment_id BIGINT UNSIGNED NULL,
    type VARCHAR(40) NOT NULL DEFAULT 'appointment',
    title VARCHAR(160) NOT NULL,
    message VARCHAR(500) NOT NULL,
    action_url VARCHAR(500) NULL,
    calendar_url VARCHAR(2000) NULL,
    is_read TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    read_at DATETIME NULL,
    PRIMARY KEY (id),
    KEY idx_app_notifications_user_read_created (user_id, is_read, created_at),
    CONSTRAINT fk_app_notifications_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_app_notifications_appointment FOREIGN KEY (appointment_id) REFERENCES appointments(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
