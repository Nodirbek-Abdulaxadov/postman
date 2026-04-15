CREATE TABLE IF NOT EXISTS branches (
    id UUID PRIMARY KEY,
    code VARCHAR(32) NOT NULL UNIQUE,
    name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    branch_id UUID NULL REFERENCES branches(id),
    role SMALLINT NOT NULL,
    full_name TEXT NOT NULL,
    phone VARCHAR(20) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_users_role CHECK (role BETWEEN 0 AND 3)
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    token_hash TEXT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_branch_role
ON users(branch_id, role)
WHERE branch_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_active
ON refresh_tokens(user_id, expires_at)
WHERE revoked_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_token_hash
ON refresh_tokens(token_hash);
