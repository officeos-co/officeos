package com.enterpriseagentos.backendjava.domain.common.primitives;

import java.util.function.Function;

public final class AgentResult<T> {
    private final T value;
    private final AgentError error;

    private AgentResult(T value, AgentError error) {
        this.value = value;
        this.error = error;
    }

    public static <T> AgentResult<T> ok(T value) {
        return new AgentResult<>(value, null);
    }

    public static <T> AgentResult<T> fail(AgentError error) {
        return new AgentResult<>(null, error);
    }

    public boolean isSuccess() {
        return error == null;
    }

    public boolean isFailure() {
        return error != null;
    }

    public T value() {
        if (isFailure()) {
            throw new IllegalStateException("Cannot access value on a failed AgentResult.");
        }
        return value;
    }

    public AgentError error() {
        if (isSuccess()) {
            throw new IllegalStateException("Cannot access error on a successful AgentResult.");
        }
        return error;
    }

    public <R> R match(Function<T, R> success, Function<AgentError, R> failure) {
        return isSuccess() ? success.apply(value) : failure.apply(error);
    }

    public <R> AgentResult<R> bind(Function<T, AgentResult<R>> next) {
        return isSuccess() ? next.apply(value) : AgentResult.fail(error);
    }
}
