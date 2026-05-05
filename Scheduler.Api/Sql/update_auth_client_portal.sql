USE scheduler_db;

ALTER TABLE users ADD COLUMN IF NOT EXISTS role VARCHAR(30) NOT NULL DEFAULT 'professional' AFTER timezone;
ALTER TABLE users ADD COLUMN IF NOT EXISTS professional_user_id BIGINT UNSIGNED NULL AFTER role;
ALTER TABLE users ADD COLUMN IF NOT EXISTS client_id BIGINT UNSIGNED NULL AFTER professional_user_id;
ALTER TABLE users ADD COLUMN IF NOT EXISTS public_slug VARCHAR(160) NULL AFTER client_id;
CREATE UNIQUE INDEX uq_users_public_slug ON users(public_slug);
