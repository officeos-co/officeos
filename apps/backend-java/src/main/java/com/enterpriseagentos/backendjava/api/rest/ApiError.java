package com.enterpriseagentos.backendjava.api.rest;

import java.time.OffsetDateTime;

public record ApiError(String code, String message, OffsetDateTime timestamp) 
{
    
}
