Feature: TfL Journey Planner Widget
  Validate core journey-planning functionality for the STA coding challenge.

  Background:
    Given I open the TfL Journey Planner

  @ValidJourney
  Scenario Outline: Plan a valid journey and verify alternative times
    When I select From "<From>" from suggestions
    And I select To "<To>" from suggestions
    And I submit the journey
    Then the cycling time should be "<CyclingTime>"
    And the walking time should be "<WalkingTime>"

    Examples:
      | From                                  | To                                | CyclingTime | WalkingTime |
      | Leicester Square Underground Station | Covent Garden Underground Station | 1 minutes   | 6 minutes   |

  @JourneyPreferences
  Scenario Outline: Apply least walking preference and verify journey time
    Given a journey is planned from "<From>" to "<To>"
    When I open Edit preferences
    And I choose routes with least walking
    And I update the journey
    Then the updated journey time should be displayed

    Examples:
      | From                                  | To                                |
      | Leicester Square Underground Station | Covent Garden Underground Station |

  @AccessInformation
  Scenario Outline: View journey details and verify access information
    Given a journey is planned from "<From>" to "<To>"
    When I open View details
    Then access information should appear for "<To>"

    Examples:
      | From                                  | To                                |
      | Leicester Square Underground Station | Covent Garden Underground Station |

  @InvalidJourney
  Scenario Outline: Invalid locations should not produce a journey
    When I enter invalid From "<From>"
    And I enter invalid To "<To>"
    And I submit the journey
    Then a location validation error should be displayed

    Examples:
      | From               | To                        |
      | InvalidLocation123 | AnotherInvalidLocation456 |

  @EmptyJourney
  Scenario: Empty locations should not produce a journey
    When I submit the journey
    Then required location validation should be displayed
