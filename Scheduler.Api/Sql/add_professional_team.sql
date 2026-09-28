USE scheduler_db;

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS team_owner_user_id BIGINT UNSIGNED NULL AFTER client_id;

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS company_id BIGINT UNSIGNED NULL;

CREATE INDEX idx_users_team_owner_user_id
    ON users (team_owner_user_id);

ALTER TABLE users
    ADD CONSTRAINT fk_users_team_owner
    FOREIGN KEY (team_owner_user_id) REFERENCES users(id) ON DELETE RESTRICT;
