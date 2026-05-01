package com.enterpriseagentos.backendjava.infrastructure.config;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;
import io.github.cdimascio.dotenv.Dotenv;
import org.springframework.context.ApplicationContextInitializer;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.core.env.MutablePropertySources;
import org.springframework.core.env.StandardEnvironment;
import org.springframework.core.env.SystemEnvironmentPropertySource;

public final class DotenvPropertySourceInitializer
    implements ApplicationContextInitializer<ConfigurableApplicationContext> {
    private static final String PROPERTY_SOURCE_NAME = "dotenv";
    private static final int PROBE_LEVELS_TO_SEARCH = 6;

    @Override
    public void initialize(ConfigurableApplicationContext applicationContext) {
        findDotenvFile().ifPresent(path -> addDotenvPropertySource(applicationContext.getEnvironment(), path));
    }

    private static java.util.Optional<Path> findDotenvFile() {
        Path directory = Path.of("").toAbsolutePath().normalize();
        for (int level = 0; level < PROBE_LEVELS_TO_SEARCH && directory != null; level++) {
            Path dotenv = directory.resolve(".env");
            if (Files.isRegularFile(dotenv)) {
                return java.util.Optional.of(dotenv);
            }
            directory = directory.getParent();
        }
        return java.util.Optional.empty();
    }
    
    private static void addDotenvPropertySource(ConfigurableEnvironment environment, Path dotenvPath) {
        Dotenv dotenv = Dotenv.configure()
            .directory(dotenvPath.getParent().toString())
            .filename(dotenvPath.getFileName().toString())
            .ignoreIfMissing()
            .load();

        Map<String, Object> properties = new LinkedHashMap<>();
        dotenv.entries().forEach(entry -> properties.put(entry.getKey(), entry.getValue()));
        if (properties.isEmpty()) {
            return;
        }

        addDatasourceAliases(properties);

        MutablePropertySources propertySources = environment.getPropertySources();
        SystemEnvironmentPropertySource propertySource =
            new SystemEnvironmentPropertySource(PROPERTY_SOURCE_NAME, properties);
        if (propertySources.contains(StandardEnvironment.SYSTEM_ENVIRONMENT_PROPERTY_SOURCE_NAME)) {
            propertySources.addAfter(StandardEnvironment.SYSTEM_ENVIRONMENT_PROPERTY_SOURCE_NAME, propertySource);
        } else {
            propertySources.addFirst(propertySource);
        }
    }

    private static void addDatasourceAliases(Map<String, Object> properties) {
        Object dbUrl = properties.get("DB_URL");
        if (dbUrl instanceof String url && !url.isBlank()) {
            putIfMissing(properties, "spring.datasource.url", url);
            putIfPresent(properties, "spring.datasource.username", properties.get("DB_USER"));
            putIfPresent(properties, "spring.datasource.password", properties.get("DB_PASSWORD"));
            if (url.startsWith("jdbc:postgresql:")) {
                putIfMissing(properties, "spring.datasource.driver-class-name", "org.postgresql.Driver");
            }
            return;
        }

        Object connectionString = properties.get("CONNECTION_STRING");
        if (connectionString instanceof String value && !value.isBlank()) {
            addPostgresDatasourceProperties(properties, value);
        }
    }

    private static void addPostgresDatasourceProperties(Map<String, Object> properties, String connectionString) {
        Map<String, String> parts = parseConnectionString(connectionString);
        String host = firstNonBlank(parts, "host", "server");
        String database = firstNonBlank(parts, "database", "initial catalog");
        if (host == null || database == null) {
            return;
        }

        String port = firstNonBlank(parts, "port");
        String username = firstNonBlank(parts, "username", "user id", "user");
        String password = firstNonBlank(parts, "password", "pwd");
        String sslMode = firstNonBlank(parts, "ssl mode", "sslmode");

        StringBuilder url = new StringBuilder("jdbc:postgresql://")
            .append(host)
            .append(':')
            .append(port == null ? "5432" : port)
            .append('/')
            .append(database);
        if (sslMode != null) {
            url.append("?sslmode=").append(sslMode.toLowerCase(Locale.ROOT).replace(" ", "-"));
        }

        putIfMissing(properties, "spring.datasource.url", url.toString());
        putIfPresent(properties, "spring.datasource.username", username);
        putIfPresent(properties, "spring.datasource.password", password);
        putIfMissing(properties, "spring.datasource.driver-class-name", "org.postgresql.Driver");
    }

    private static Map<String, String> parseConnectionString(String connectionString) {
        Map<String, String> parts = new LinkedHashMap<>();
        for (String part : connectionString.split(";")) {
            int equalsIndex = part.indexOf('=');
            if (equalsIndex <= 0) {
                continue;
            }

            String key = part.substring(0, equalsIndex).trim().toLowerCase(Locale.ROOT);
            String value = part.substring(equalsIndex + 1).trim();
            if (!key.isEmpty() && !value.isEmpty()) {
                parts.put(key, value);
            }
        }
        return parts;
    }

    private static String firstNonBlank(Map<String, String> values, String... keys) {
        for (String key : keys) {
            String value = values.get(key);
            if (value != null && !value.isBlank()) {
                return value;
            }
        }
        return null;
    }

    private static void putIfPresent(Map<String, Object> properties, String key, Object value) {
        if (value instanceof String stringValue && !stringValue.isBlank()) {
            putIfMissing(properties, key, stringValue);
        }
    }

    private static void putIfMissing(Map<String, Object> properties, String key, Object value) {
        properties.putIfAbsent(key, value);
    }
}
