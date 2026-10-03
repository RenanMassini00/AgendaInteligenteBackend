USE scheduler_db;

CREATE TABLE IF NOT EXISTS appointment_payments (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    appointment_id BIGINT UNSIGNED NOT NULL,
    account_owner_user_id BIGINT UNSIGNED NOT NULL,
    public_reference CHAR(36) NOT NULL,
    provider_payment_id VARCHAR(40) NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    amount DECIMAL(10,2) NOT NULL,
    qr_code LONGTEXT NULL,
    qr_code_base64 LONGTEXT NULL,
    expires_at DATETIME NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_appointment_payments_appointment_id (appointment_id),
    UNIQUE KEY uq_appointment_payments_public_reference (public_reference),
    UNIQUE KEY uq_appointment_payments_provider_payment_id (provider_payment_id),
    KEY idx_appointment_payments_status_expires_at (status, expires_at),
    KEY idx_appointment_payments_account_owner (account_owner_user_id),
    CONSTRAINT fk_appointment_payments_appointment
        FOREIGN KEY (appointment_id) REFERENCES appointments(id) ON DELETE CASCADE,
    CONSTRAINT fk_appointment_payments_account_owner
        FOREIGN KEY (account_owner_user_id) REFERENCES users(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS mercado_pago_accounts (
    user_id BIGINT UNSIGNED NOT NULL,
    mercado_pago_user_id VARCHAR(32) NOT NULL,
    access_token_encrypted LONGTEXT NOT NULL,
    refresh_token_encrypted LONGTEXT NOT NULL,
    expires_at DATETIME NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id),
    CONSTRAINT fk_mercado_pago_accounts_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS mercado_pago_oauth_states (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id BIGINT UNSIGNED NOT NULL,
    state_hash CHAR(64) NOT NULL,
    expires_at DATETIME NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mercado_pago_oauth_states_hash (state_hash),
    KEY idx_mercado_pago_oauth_states_expires_at (expires_at),
    CONSTRAINT fk_mercado_pago_oauth_states_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
