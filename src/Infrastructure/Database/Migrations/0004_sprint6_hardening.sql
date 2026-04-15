-- Sprint 6 Hardening: audit log table, cleanup index, courier location partition for current month

-- Audit log for critical operations (assignment, status transitions, cancellations, admin actions)
CREATE TABLE IF NOT EXISTS audit_logs (
    id          BIGSERIAL PRIMARY KEY,
    actor_id    UUID          NOT NULL,
    actor_role  SMALLINT      NOT NULL,
    action      VARCHAR(64)   NOT NULL,
    entity_type VARCHAR(64)   NOT NULL,
    entity_id   UUID          NOT NULL,
    branch_id   UUID          NULL,
    detail      TEXT          NULL,
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_logs_entity
    ON audit_logs (entity_type, entity_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_audit_logs_actor
    ON audit_logs (actor_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_audit_logs_branch
    ON audit_logs (branch_id, created_at DESC)
    WHERE branch_id IS NOT NULL;

-- Index for the refresh token cleanup worker (fast batch deletes)
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_cleanup
    ON refresh_tokens (expires_at, revoked_at)
    WHERE revoked_at IS NULL OR revoked_at IS NOT NULL;

-- Courier location monthly partition for the current month (run manually for each month)
-- Example: create partition for 2026-04 if it does not exist
DO $outer$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = 'courier_locations_y2026m04'
          AND n.nspname = 'public'
    ) THEN
        EXECUTE $q$
            CREATE TABLE courier_locations_y2026m04
                PARTITION OF courier_locations
                FOR VALUES FROM ('2026-04-01') TO ('2026-05-01')
        $q$;
    END IF;
END;
$outer$;

DO $outer$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = 'courier_locations_y2026m05'
          AND n.nspname = 'public'
    ) THEN
        EXECUTE $q$
            CREATE TABLE courier_locations_y2026m05
                PARTITION OF courier_locations
                FOR VALUES FROM ('2026-05-01') TO ('2026-06-01')
        $q$;
    END IF;
END;
$outer$;

DO $outer$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = 'courier_locations_default'
          AND n.nspname = 'public'
    ) THEN
        EXECUTE $q$
            CREATE TABLE courier_locations_default
                PARTITION OF courier_locations DEFAULT
        $q$;
    END IF;
END;
$outer$;
