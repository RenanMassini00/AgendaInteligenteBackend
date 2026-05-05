USE scheduler_db;

INSERT INTO users (full_name, business_name, email, phone, password_hash, specialty, role, public_slug)
VALUES ('Renan', 'Renan Studio', 'renan@email.com', '11999999999', '123456', 'Profissional Autônomo', 'professional', 'renan-studio')
ON DUPLICATE KEY UPDATE full_name = VALUES(full_name), business_name = VALUES(business_name), specialty = VALUES(specialty), role = VALUES(role), public_slug = VALUES(public_slug), password_hash = VALUES(password_hash);

INSERT INTO user_settings (user_id, theme, language_code, reminder_minutes, email_notifications, whatsapp_notifications)
VALUES (1, 'light', 'pt-BR', 60, 0, 1)
ON DUPLICATE KEY UPDATE theme = VALUES(theme), language_code = VALUES(language_code), reminder_minutes = VALUES(reminder_minutes), email_notifications = VALUES(email_notifications), whatsapp_notifications = VALUES(whatsapp_notifications);

INSERT INTO clients (id, user_id, full_name, email, phone, notes) VALUES
(1, 1, 'Mariana Costa', 'mariana@email.com', '11999990001', 'Cliente frequente'),
(2, 1, 'Lucas Ferreira', 'lucas@email.com', '11999990002', 'Prefere atendimento de manhã'),
(3, 1, 'Patrícia Lima', 'patricia@email.com', '11999990003', 'Atendimento mensal'),
(4, 1, 'André Souza', 'andre@email.com', '11999990004', 'Cliente novo')
ON DUPLICATE KEY UPDATE full_name = VALUES(full_name), email = VALUES(email), phone = VALUES(phone), notes = VALUES(notes);

INSERT INTO users (full_name, business_name, email, phone, password_hash, specialty, role, professional_user_id, client_id)
VALUES ('Mariana Costa', NULL, 'cliente@email.com', '11999990001', '123456', NULL, 'client', 1, 1)
ON DUPLICATE KEY UPDATE full_name = VALUES(full_name), role = VALUES(role), professional_user_id = VALUES(professional_user_id), client_id = VALUES(client_id), password_hash = VALUES(password_hash);

INSERT INTO services (id, user_id, name, description, duration_minutes, price, color_hex) VALUES
(1, 1, 'Corte feminino', 'Corte completo', 60, 90.00, '#3B82F6'),
(2, 1, 'Treino funcional', 'Atendimento funcional individual', 60, 120.00, '#F59E0B'),
(3, 1, 'Manicure', 'Atendimento padrão', 40, 50.00, '#10B981'),
(4, 1, 'Design de sobrancelha', 'Design completo', 30, 45.00, '#8B5CF6')
ON DUPLICATE KEY UPDATE name = VALUES(name), description = VALUES(description), duration_minutes = VALUES(duration_minutes), price = VALUES(price), color_hex = VALUES(color_hex);

INSERT INTO weekly_availabilities (user_id, weekday, start_time, end_time) VALUES
(1, 1, '08:00:00', '18:00:00'),
(1, 2, '08:00:00', '18:00:00'),
(1, 3, '08:00:00', '18:00:00'),
(1, 4, '08:00:00', '18:00:00'),
(1, 5, '08:00:00', '17:00:00'),
(1, 6, '08:00:00', '12:00:00');

INSERT INTO appointments (id, user_id, client_id, service_id, appointment_date, start_time, end_time, status, price_at_booking, notes) VALUES
(1, 1, 1, 1, CURDATE(), '09:00:00', '10:00:00', 'confirmed', 90.00, 'Primeiro atendimento'),
(2, 1, 2, 2, CURDATE(), '10:30:00', '11:30:00', 'scheduled', 120.00, 'Confirmar no WhatsApp'),
(3, 1, 3, 3, CURDATE(), '14:00:00', '14:40:00', 'completed', 50.00, NULL),
(4, 1, 4, 4, DATE_ADD(CURDATE(), INTERVAL 1 DAY), '08:30:00', '09:00:00', 'scheduled', 45.00, NULL)
ON DUPLICATE KEY UPDATE status = VALUES(status), price_at_booking = VALUES(price_at_booking), notes = VALUES(notes);
