use super::traits::{Observer, ObserverEvent, ObserverMetric};
use std::any::Any;

/// Combine multiple observers — fan-out events to all backends
pub struct MultiObserver {
    observers: Vec<Box<dyn Observer>>,
}

impl MultiObserver {
    pub fn new(observers: Vec<Box<dyn Observer>>) -> Self {
        Self { observers }
    }
}

impl Observer for MultiObserver {
    fn record_event(&self, event: &ObserverEvent) {
        for obs in &self.observers {
            obs.record_event(event);
        }
    }

    fn record_metric(&self, metric: &ObserverMetric) {
        for obs in &self.observers {
            obs.record_metric(metric);
        }
    }

    fn flush(&self) {
        for obs in &self.observers {
            obs.flush();
        }
    }

    fn name(&self) -> &str {
        "multi"
    }

    fn as_any(&self) -> &dyn Any {
        self
    }
}


#[cfg(test)]
#[path = "multi.test.rs"]
mod tests;
