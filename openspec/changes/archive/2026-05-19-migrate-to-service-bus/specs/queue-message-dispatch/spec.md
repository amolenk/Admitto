## ADDED Requirements

### Requirement: At-least-once message delivery with explicit settlement
The queue consumer SHALL use a push-based delivery mechanism. Messages SHALL be explicitly completed after successful dispatch, and abandoned (left for retry) on failure. The system SHALL guarantee at-least-once delivery: if the worker crashes after dispatch but before completion, the message will be redelivered.

#### Scenario: Message is completed after successful dispatch
- **WHEN** a message is received and all handlers dispatch without error
- **THEN** the message is explicitly completed (removed from the queue)

#### Scenario: Message is abandoned on dispatch failure
- **WHEN** a message is received and processing throws an exception
- **THEN** the message is abandoned and becomes available for redelivery
