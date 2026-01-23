-- Aurora Livecam - Multi-Tenant SaaS Schema
-- Version: 1.0.0

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- --------------------------------------------------------
-- Subscription Plans
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `plans` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `slug` VARCHAR(50) UNIQUE NOT NULL,
    `stripe_price_id` VARCHAR(100) NULL,
    `price_monthly` DECIMAL(10,2) DEFAULT 0.00,
    `price_yearly` DECIMAL(10,2) DEFAULT 0.00,
    `features` JSON NULL COMMENT '{"max_viewers": 100, "storage_gb": 5, "custom_domain": true}',
    `is_active` TINYINT(1) DEFAULT 1,
    `sort_order` INT DEFAULT 0,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Default Plans
INSERT INTO `plans` (`name`, `slug`, `price_monthly`, `price_yearly`, `features`, `sort_order`) VALUES
('Free', 'free', 0.00, 0.00, '{"max_viewers": 10, "storage_gb": 0.5, "custom_domain": false, "weather_widget": true, "timelapse": false, "analytics": false, "branding": false}', 1),
('Basic', 'basic', 19.00, 190.00, '{"max_viewers": 50, "storage_gb": 5, "custom_domain": false, "weather_widget": true, "timelapse": true, "analytics": true, "branding": false}', 2),
('Professional', 'professional', 49.00, 490.00, '{"max_viewers": 200, "storage_gb": 20, "custom_domain": true, "weather_widget": true, "timelapse": true, "analytics": true, "branding": true}', 3),
('Enterprise', 'enterprise', 149.00, 1490.00, '{"max_viewers": -1, "storage_gb": 100, "custom_domain": true, "weather_widget": true, "timelapse": true, "analytics": true, "branding": true, "priority_support": true}', 4);

-- --------------------------------------------------------
-- Tenants (Customers)
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenants` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `uuid` VARCHAR(36) UNIQUE NOT NULL,
    `name` VARCHAR(255) NOT NULL,
    `slug` VARCHAR(100) UNIQUE NOT NULL COMMENT 'URL-safe identifier, e.g. aurora, seecam',
    `email` VARCHAR(255) NOT NULL,
    `status` ENUM('trial', 'active', 'suspended', 'cancelled') DEFAULT 'trial',
    `plan_id` INT UNSIGNED NULL,
    `trial_ends_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`plan_id`) REFERENCES `plans`(`id`) ON DELETE SET NULL,
    INDEX `idx_status` (`status`),
    INDEX `idx_slug` (`slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Tenant Domains
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenant_domains` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `domain` VARCHAR(255) UNIQUE NOT NULL,
    `is_primary` TINYINT(1) DEFAULT 0,
    `ssl_status` ENUM('pending', 'active', 'failed') DEFAULT 'pending',
    `verified_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE,
    INDEX `idx_domain` (`domain`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Tenant Settings (replaces settings.json per tenant)
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenant_settings` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `setting_key` VARCHAR(255) NOT NULL,
    `setting_value` TEXT NULL,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_tenant_key` (`tenant_id`, `setting_key`),
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Tenant Branding
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenant_branding` (
    `tenant_id` INT UNSIGNED PRIMARY KEY,
    `site_name` VARCHAR(255) NULL,
    `site_name_full` VARCHAR(255) NULL,
    `tagline` VARCHAR(255) NULL,
    `logo_path` VARCHAR(500) NULL,
    `favicon_path` VARCHAR(500) NULL,
    `primary_color` VARCHAR(7) DEFAULT '#667eea',
    `secondary_color` VARCHAR(7) DEFAULT '#764ba2',
    `accent_color` VARCHAR(7) DEFAULT '#f093fb',
    `welcome_text_de` TEXT NULL,
    `welcome_text_en` TEXT NULL,
    `footer_text` TEXT NULL,
    `custom_css` TEXT NULL,
    `custom_js` TEXT NULL,
    `social_facebook` VARCHAR(255) NULL,
    `social_instagram` VARCHAR(255) NULL,
    `social_youtube` VARCHAR(255) NULL,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Tenant Streams
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenant_streams` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `name` VARCHAR(255) DEFAULT 'Main Stream',
    `stream_url` VARCHAR(500) NOT NULL,
    `stream_type` ENUM('hls', 'rtmp', 'webrtc', 'iframe') DEFAULT 'hls',
    `is_active` TINYINT(1) DEFAULT 1,
    `is_primary` TINYINT(1) DEFAULT 1,
    `last_check_at` TIMESTAMP NULL,
    `last_status` ENUM('online', 'offline', 'error') NULL,
    `error_message` VARCHAR(500) NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Users
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `users` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NULL COMMENT 'NULL = Super Admin',
    `email` VARCHAR(255) UNIQUE NOT NULL,
    `password_hash` VARCHAR(255) NOT NULL,
    `name` VARCHAR(255) NULL,
    `role` ENUM('super_admin', 'tenant_admin', 'tenant_user') NOT NULL DEFAULT 'tenant_user',
    `email_verified_at` TIMESTAMP NULL,
    `last_login_at` TIMESTAMP NULL,
    `remember_token` VARCHAR(100) NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE,
    INDEX `idx_email` (`email`),
    INDEX `idx_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Subscriptions
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `subscriptions` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `plan_id` INT UNSIGNED NOT NULL,
    `stripe_subscription_id` VARCHAR(100) NULL,
    `stripe_customer_id` VARCHAR(100) NULL,
    `status` ENUM('trialing', 'active', 'past_due', 'canceled', 'unpaid', 'incomplete') DEFAULT 'trialing',
    `current_period_start` TIMESTAMP NULL,
    `current_period_end` TIMESTAMP NULL,
    `canceled_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`plan_id`) REFERENCES `plans`(`id`),
    INDEX `idx_tenant` (`tenant_id`),
    INDEX `idx_stripe_sub` (`stripe_subscription_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Invoices (Stripe cache)
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `invoices` (
    `id` INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `stripe_invoice_id` VARCHAR(100) UNIQUE NULL,
    `amount` DECIMAL(10,2) NOT NULL,
    `currency` VARCHAR(3) DEFAULT 'CHF',
    `status` VARCHAR(50) NULL,
    `paid_at` TIMESTAMP NULL,
    `invoice_pdf_url` VARCHAR(500) NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Viewer Statistics
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `viewer_stats` (
    `id` BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `tenant_id` INT UNSIGNED NOT NULL,
    `recorded_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `viewer_count` INT DEFAULT 0,
    `unique_sessions` INT DEFAULT 0,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE,
    INDEX `idx_tenant_time` (`tenant_id`, `recorded_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Onboarding Progress
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tenant_onboarding` (
    `tenant_id` INT UNSIGNED PRIMARY KEY,
    `current_step` INT DEFAULT 1,
    `stream_verified` TINYINT(1) DEFAULT 0,
    `branding_configured` TINYINT(1) DEFAULT 0,
    `payment_configured` TINYINT(1) DEFAULT 0,
    `completed_at` TIMESTAMP NULL,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`tenant_id`) REFERENCES `tenants`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
