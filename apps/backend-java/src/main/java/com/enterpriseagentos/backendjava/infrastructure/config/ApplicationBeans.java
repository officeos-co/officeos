package com.enterpriseagentos.backendjava.infrastructure.config;

import java.time.Clock;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class ApplicationBeans {
    @Bean
    public Clock clock() {
        return Clock.systemUTC();
    }
}
