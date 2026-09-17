USE scheduler_db;

CREATE TABLE IF NOT EXISTS product_images (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    product_id BIGINT UNSIGNED NOT NULL,
    image_url VARCHAR(2000) NOT NULL,
    sort_order INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_product_images_product_order (product_id, sort_order),
    CONSTRAINT fk_product_images_product
        FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO product_images (product_id, image_url, sort_order)
SELECT id, image_url, 0
FROM products
WHERE image_url IS NOT NULL
  AND TRIM(image_url) <> ''
  AND NOT EXISTS (
      SELECT 1
      FROM product_images existing
      WHERE existing.product_id = products.id
  );
