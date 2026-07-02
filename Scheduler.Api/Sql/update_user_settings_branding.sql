USE scheduler_db;

ALTER TABLE user_settings
    ADD COLUMN IF NOT EXISTS accent_color VARCHAR(20) NOT NULL DEFAULT 'blue' AFTER updated_at;

ALTER TABLE user_settings
    ADD COLUMN IF NOT EXISTS company_logo_url LONGTEXT NULL AFTER accent_color;

ALTER TABLE user_settings
    MODIFY COLUMN company_logo_url LONGTEXT NULL;
