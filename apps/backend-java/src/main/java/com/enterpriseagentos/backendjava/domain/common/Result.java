package com.enterpriseagentos.backendjava.domain.common;

import java.util.Objects;
import java.util.function.Function;

public final class Result<T> {
    private final T value;
    private final Failure failure;

    private Result(T value, Failure failure) {
        this.value = value;
        this.failure = failure;
    }

    public static <T> Result<T> success(T value) {
        return new Result<>(Objects.requireNonNull(value), null);
    }

    public static <T> Result<T> failure(String code, String message) {
        return new Result<>(null, new Failure(code, message));
    }

    public boolean isSuccess() {
        return failure == null;
    }

    public T value() {
        if (!isSuccess()) {
            throw new IllegalStateException("Cannot read value from failed result");
        }
        return value;
    }

    public Failure failure() {
        if (isSuccess()) {
            throw new IllegalStateException("Cannot read failure from successful result");
        }
        return failure;
    }

    public <R> R fold(Function<T, R> onSuccess, Function<Failure, R> onFailure) {
        return isSuccess() ? onSuccess.apply(value) : onFailure.apply(failure);
    }

    public record Failure(String code, String message) {
        public Failure {
            Objects.requireNonNull(code);
            Objects.requireNonNull(message);
        }
    }
}
