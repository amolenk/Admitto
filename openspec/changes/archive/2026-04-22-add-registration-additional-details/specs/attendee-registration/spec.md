## ADDED Requirements

### Requirement: Self-registration accepts and validates additional detail values
The self-registration command and public endpoint SHALL accept an optional `additionalDetails` map of `string` keys to `string` values. The handler SHALL validate the map against the event's current `AdditionalDetailSchema` (see event-management). All additional detail values SHALL be optional at the platform layer; missing keys SHALL be treated as not provided and SHALL NOT cause a rejection.

The handler SHALL reject the registration when the map contains a key that is not present in the current schema (`AdditionalDetailKeyNotInSchema`), or when any value's length exceeds the field's `MaxLength` (`AdditionalDetailValueTooLong`). Empty-string values SHALL be accepted and stored verbatim.

Accepted values SHALL be stored on the resulting `Registration` aggregate (see registration-additional-details for the storage model).

#### Scenario: Self-service accepts additional details matching the schema
- **WHEN** an attendee self-registers for event "DevConf" whose schema declares `dietary` (maxLength 200) and `tshirt` (maxLength 5), submitting `{ "dietary": "vegan", "tshirt": "M" }`
- **THEN** the registration is created and the values are stored

#### Scenario: Self-service accepts when additional details are omitted
- **WHEN** an attendee self-registers for "DevConf" without sending any `additionalDetails`
- **THEN** the registration is created with no additional detail values

#### Scenario: Self-service accepts a partial set of declared keys
- **WHEN** an attendee self-registers for "DevConf" with only `{ "dietary": "vegan" }`
- **THEN** the registration is created and `tshirt` is recorded as not provided

#### Scenario: Self-service accepts empty-string values
- **WHEN** an attendee self-registers for "DevConf" with `{ "dietary": "" }`
- **THEN** the registration is created and `dietary` is stored as the empty string

#### Scenario: Self-service rejected — unknown key
- **WHEN** an attendee self-registers for "DevConf" with `{ "shoesize": "44" }` and the schema has no `shoesize` field
- **THEN** the registration is rejected with reason "additional detail key not in schema"

#### Scenario: Self-service rejected — value too long
- **WHEN** an attendee self-registers for "DevConf" with `{ "tshirt": "XXXXL-extra-long" }` and the `tshirt` field has `maxLength: 5`
- **THEN** the registration is rejected with reason "additional detail value too long"

---

### Requirement: Coupon registration accepts and validates additional detail values
The coupon registration command and public endpoint SHALL accept the same optional `additionalDetails` map and apply the same validation rules described in "Self-registration accepts and validates additional detail values". Coupon-based registrations SHALL NOT bypass additional-detail validation; the schema applies regardless of coupon usage.

#### Scenario: Coupon registration accepts additional details
- **WHEN** an attendee redeems coupon "EARLYBIRD" for event "DevConf" and submits `{ "dietary": "vegan" }`
- **THEN** the registration is created with the values stored

#### Scenario: Coupon registration rejected — unknown key
- **WHEN** an attendee redeems a coupon for "DevConf" with `{ "shoesize": "44" }` and the schema has no `shoesize` field
- **THEN** the registration is rejected with reason "additional detail key not in schema"
