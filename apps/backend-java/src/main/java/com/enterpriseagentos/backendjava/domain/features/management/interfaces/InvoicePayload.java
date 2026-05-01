package com.enterpriseagentos.backendjava.domain.features.management.interfaces;

import com.enterpriseagentos.backendjava.domain.common.*;
import com.enterpriseagentos.backendjava.domain.common.models.*;
import com.enterpriseagentos.backendjava.domain.common.primitives.*;
import com.enterpriseagentos.backendjava.domain.common.services.*;
import com.enterpriseagentos.backendjava.domain.common.valueobjects.*;
import com.enterpriseagentos.backendjava.domain.events.*;
import com.enterpriseagentos.backendjava.domain.features.agents.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.agents.enums.*;
import com.enterpriseagentos.backendjava.domain.features.agents.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.agents.models.*;
import com.enterpriseagentos.backendjava.domain.features.agents.registries.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.enums.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.mappers.*;
import com.enterpriseagentos.backendjava.domain.features.analytics.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.management.exceptions.*;
import com.enterpriseagentos.backendjava.domain.features.management.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.management.models.*;
import com.enterpriseagentos.backendjava.domain.features.management.registries.*;
import com.enterpriseagentos.backendjava.domain.features.management.services.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.dtos.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.enums.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.interfaces.*;
import com.enterpriseagentos.backendjava.domain.features.mcp.models.*;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.YearMonth;
import java.util.*;
import java.util.concurrent.CompletableFuture;

public final class InvoicePayload  {
    private final String id;
    private final Instant date;
    private final String total;
    private final String currency;
    private final String status;
    private final String hostedUrl;
    private final String pdfUrl;

    public InvoicePayload(String id, Instant date, String total, String currency, String status, String hostedUrl, String pdfUrl) {
        this.id = id;
        this.date = date;
        this.total = total;
        this.currency = currency;
        this.status = status;
        this.hostedUrl = hostedUrl;
        this.pdfUrl = pdfUrl;
    }

    public String getId() {
        return id;
}

    public String id() {
        return id;
    }

    public Instant getDate() {
        return date;
}

    public Instant date() {
        return date;
    }

    public String getTotal() {
        return total;
}

    public String total() {
        return total;
    }

    public String getCurrency() {
        return currency;
}

    public String currency() {
        return currency;
    }

    public String getStatus() {
        return status;
}

    public String status() {
        return status;
    }

    public String getHostedUrl() {
        return hostedUrl;
}

    public String hostedUrl() {
        return hostedUrl;
    }

    public String getPdfUrl() {
        return pdfUrl;
}

    public String pdfUrl() {
        return pdfUrl;
    }
}
