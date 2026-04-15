CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY,
    order_code VARCHAR(32) NOT NULL UNIQUE,
    customer_id UUID NOT NULL REFERENCES users(id),
    branch_id UUID NOT NULL REFERENCES branches(id),
    courier_id UUID NULL REFERENCES users(id),
    status SMALLINT NOT NULL,
    recipient_name TEXT NOT NULL,
    recipient_phone VARCHAR(20) NOT NULL,
    address TEXT NOT NULL,
    lat DOUBLE PRECISION NOT NULL,
    lng DOUBLE PRECISION NOT NULL,
    assigned_at TIMESTAMPTZ NULL,
    delivered_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_orders_status CHECK (status BETWEEN 0 AND 6)
);

CREATE TABLE IF NOT EXISTS order_status_history (
    id BIGSERIAL PRIMARY KEY,
    order_id UUID NOT NULL REFERENCES orders(id),
    from_status SMALLINT NULL,
    to_status SMALLINT NOT NULL,
    changed_by_user_id UUID NOT NULL REFERENCES users(id),
    note TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_order_history_to_status CHECK (to_status BETWEEN 0 AND 6)
);

CREATE INDEX IF NOT EXISTS idx_orders_branch_status_created
ON orders(branch_id, status, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_orders_courier_status
ON orders(courier_id, status)
WHERE courier_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_orders_customer_created
ON orders(customer_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_order_status_history_order_time
ON order_status_history(order_id, created_at DESC);
