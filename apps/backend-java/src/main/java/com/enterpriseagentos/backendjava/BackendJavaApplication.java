package com.enterpriseagentos.backendjava;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

import com.enterpriseagentos.backendjava.infrastructure.config.DotenvPropertySourceInitializer;

@SpringBootApplication
public class BackendJavaApplication {
    public static void main(String[] args) {
        SpringApplication application = new SpringApplication(BackendJavaApplication.class);
        application.addInitializers(new DotenvPropertySourceInitializer());
        application.run(args);
    }
}
