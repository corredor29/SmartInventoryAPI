
CREATE EXTENSION IF NOT EXISTS vector;


CREATE TABLE role (
    role_id     SERIAL PRIMARY KEY,
    name        VARCHAR(50) NOT NULL UNIQUE  
);

CREATE TABLE customer (
    customer_id     SERIAL PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    email           VARCHAR(150),
    phone           VARCHAR(30),
    document_number VARCHAR(50),
    created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE "user" (
    user_id         SERIAL PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    email           VARCHAR(150) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    role_id         INT NOT NULL REFERENCES role(role_id),
    customer_id     INT REFERENCES customer(customer_id),  
    created_at      TIMESTAMP NOT NULL DEFAULT now()
);



CREATE TABLE category (
    category_id     SERIAL PRIMARY KEY,
    name            VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE product_status (
    product_status_id  SERIAL PRIMARY KEY,
    name                VARCHAR(50) NOT NULL UNIQUE  
);

CREATE TABLE product (
    product_id          SERIAL PRIMARY KEY,
    name                VARCHAR(150) NOT NULL,
    description         TEXT,
    price               NUMERIC(12,2) NOT NULL CHECK (price >= 0),
    category_id         INT NOT NULL REFERENCES category(category_id),
    product_status_id   INT NOT NULL REFERENCES product_status(product_status_id),
    embedding           vector(1536),  
    created_at          TIMESTAMP NOT NULL DEFAULT now()
);

CREATE INDEX idx_product_embedding ON product USING ivfflat (embedding vector_cosine_ops);

-- =========================================================
-- 3. INVENTARIO
-- =========================================================

CREATE TABLE inventory (
    inventory_id    SERIAL PRIMARY KEY,
    product_id      INT NOT NULL UNIQUE REFERENCES product(product_id),
    current_stock   INT NOT NULL DEFAULT 0 CHECK (current_stock >= 0)
);

CREATE TABLE movement_type (
    movement_type_id    SERIAL PRIMARY KEY,
    name                 VARCHAR(50) NOT NULL UNIQUE  
);

CREATE TABLE inventory_movement (
    movement_id         SERIAL PRIMARY KEY,
    inventory_id        INT NOT NULL REFERENCES inventory(inventory_id),
    movement_type_id    INT NOT NULL REFERENCES movement_type(movement_type_id),
    quantity             INT NOT NULL CHECK (quantity > 0),
    reason               VARCHAR(255),
    created_at           TIMESTAMP NOT NULL DEFAULT now()
);


CREATE TABLE sale_origin (
    sale_origin_id  SERIAL PRIMARY KEY,
    name             VARCHAR(50) NOT NULL UNIQUE  
);

CREATE TABLE sale_status (
    sale_status_id  SERIAL PRIMARY KEY,
    name             VARCHAR(50) NOT NULL UNIQUE  
);

CREATE TABLE sale (
    sale_id          SERIAL PRIMARY KEY,
    customer_id      INT NOT NULL REFERENCES customer(customer_id),
    sale_origin_id   INT NOT NULL REFERENCES sale_origin(sale_origin_id),
    sale_status_id   INT NOT NULL REFERENCES sale_status(sale_status_id),
    sale_date        TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE sale_detail (
    sale_detail_id  SERIAL PRIMARY KEY,
    sale_id          INT NOT NULL REFERENCES sale(sale_id),
    product_id       INT NOT NULL REFERENCES product(product_id),
    quantity         INT NOT NULL CHECK (quantity > 0),
    unit_price       NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0)  -- precio congelado al momento de la venta
);

CREATE TABLE invoice (
    invoice_id       SERIAL PRIMARY KEY,
    sale_id           INT NOT NULL UNIQUE REFERENCES sale(sale_id),
    invoice_number    VARCHAR(20) NOT NULL UNIQUE,  -- ej. FAC-000001
    issue_date        TIMESTAMP NOT NULL DEFAULT now()
);

-- =========================================================
-- 5. CHATBOT Y ESCALAMIENTO A ASESOR
-- =========================================================

CREATE TABLE chat_session_status (
    chat_session_status_id  SERIAL PRIMARY KEY,
    name                      VARCHAR(50) NOT NULL UNIQUE  -- Activa / Escalada / Cerrada
);

CREATE TABLE chat_session (
    chat_session_id          SERIAL PRIMARY KEY,
    customer_id               INT REFERENCES customer(customer_id),  -- null si es anónimo
    chat_session_status_id    INT NOT NULL REFERENCES chat_session_status(chat_session_status_id),
    started_at                 TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE sender_type (
    sender_type_id   SERIAL PRIMARY KEY,
    name              VARCHAR(50) NOT NULL UNIQUE  -- Bot / Cliente / Asesor
);

CREATE TABLE chat_message (
    chat_message_id   SERIAL PRIMARY KEY,
    chat_session_id    INT NOT NULL REFERENCES chat_session(chat_session_id),
    sender_type_id      INT NOT NULL REFERENCES sender_type(sender_type_id),
    content              TEXT NOT NULL,
    sent_at              TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE escalation_status (
    escalation_status_id  SERIAL PRIMARY KEY,
    name                    VARCHAR(50) NOT NULL UNIQUE  -- Pendiente / En Progreso / Resuelto
);

CREATE TABLE chat_escalation (
    chat_escalation_id    SERIAL PRIMARY KEY,
    chat_session_id         INT NOT NULL UNIQUE REFERENCES chat_session(chat_session_id),
    reason                   TEXT,
    escalation_status_id     INT NOT NULL REFERENCES escalation_status(escalation_status_id),
    assigned_user_id         INT REFERENCES "user"(user_id),  -- debe tener role = Asesor
    created_at                TIMESTAMP NOT NULL DEFAULT now(),
    resolved_at               TIMESTAMP
);

