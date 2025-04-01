CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE pizza_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    content TEXT NOT NULL,
    order_number TEXT,
    toppings TEXT[]
);

CREATE TABLE sourdough_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    content TEXT NOT NULL,
    status TEXT,
    rising_time INTEGER
);