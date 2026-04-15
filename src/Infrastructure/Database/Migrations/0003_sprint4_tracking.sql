CREATE TABLE IF NOT EXISTS courier_locations (
    id BIGSERIAL NOT NULL,
    courier_id UUID NOT NULL REFERENCES users(id),
    order_id UUID NULL REFERENCES orders(id),
    lat DOUBLE PRECISION NOT NULL,
    lng DOUBLE PRECISION NOT NULL,
    accuracy_meters REAL NULL,
    speed_mps REAL NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

CREATE TABLE IF NOT EXISTS courier_locations_default
PARTITION OF courier_locations DEFAULT;

CREATE INDEX IF NOT EXISTS idx_courier_locations_courier_time
ON courier_locations(courier_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_courier_locations_order_time
ON courier_locations(order_id, created_at DESC)
WHERE order_id IS NOT NULL;
