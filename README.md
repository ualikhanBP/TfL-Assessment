# TfL Journey Planner Automation (Playwright + Reqnroll + NUnit)

This project automates key user journeys on the TfL Journey Planner using:
- **Microsoft.Playwright** for browser automation
- **Reqnroll** (SpecFlow-compatible BDD framework) for Gherkin scenarios
- **NUnit** as the test runner and assertion library

It follows a **Page Object Model (POM)** structure to keep UI locators and actions separate from test logic.

---

## Tech Stack / Packages

From the solution setup:

- **.NET**: Microsoft.NETCore.App
- **Microsoft.Playwright**: 1.58.0
- **NUnit**: 4.4.0
- **NUnit3TestAdapter**: 5.1.0
- **Microsoft.NET.Test.Sdk**: 17.14.1
- **Reqnroll.NUnit**: 3.2.0

---

## Project Structure

TFlProject/
│
├── Drivers/
│ └── PlaywrightDriver.cs
│
├── Features/
│ └── JourneyPlanner.feature
│
├── Hooks/
│ └── TestHooks.cs
│
├── Pages/
│ ├── JourneyPlannerPage.cs
│ ├── JourneyResultsPage.cs
│ └── JourneyDetailsPage.cs
│
├── StepDefinitions/
│ └── JourneyPlannerSteps.cs
│
├
│
└── reqnroll.json
└── README.md

---

## What Each Folder Does

### 1) Drivers/
Contains the browser bootstrap and lifecycle handling.

- **PlaywrightDriver.cs**
  - Creates the Playwright instance
  - Launches Chromium
  - Creates BrowserContext and Page
  - Applies timeouts
  - Disposes resources after tests



### 2) Features/
Contains all BDD scenarios in Gherkin.

- **JourneyPlanner.feature**
  - Written in `Given / When / Then`
  - Describes test behaviour in business language
  - Each line maps to a method in `JourneyPlannerSteps.cs`

---

### 3) Hooks/
Reqnroll hooks that run before/after scenarios to set up and tear down test state.

- **TestHooks.cs**
  - Starts the browser before each scenario (or once per test run depending on your hook design)
  - Stores `PlaywrightDriver` into `ScenarioContext`
  - Cleans up Playwright after each scenario
  - Best place for cross-cutting setup like:
    - headless/headed mode
    - tracing/video
    - screenshots on failure
    - global cookie banner handling (optional)

---

### 4) Pages/
Page Object Model (POM) layer:
- Keeps locators + UI operations here
- Keeps steps readable and stable

#### a) JourneyPlannerPage.cs
Represents the **TfL planner form** page.

Typical responsibilities:
- Navigate to base URL
- Handle cookie banner (if implemented here)
- Enter “From” and “To”
- Auto-suggest selection logic
- Click “Plan my journey”
- Read inline validation errors (if they appear on this page)

#### b) JourneyResultsPage.cs
Represents the **results** page after planning a journey.

Typical responsibilities:
- Detect when results are present
- Extract walking/cycling durations (where applicable)
- Open “Edit preferences”
- Select “Routes with least walking”
- Apply update and wait for refresh
- Extract total duration from the results area

#### c) JourneyDetailsPage.cs
Represents the **details** panel / expanded journey details UI.

Typical responsibilities:
- Click “View details”
- Verify station-specific details exist (e.g., access information)
- Locate and validate accessibility icons / access details section

---

### 5) StepDefinitions/
BDD glue code that links Feature steps to automation logic.

- **JourneyPlannerSteps.cs**
  - Implements methods for each Gherkin step
  - Calls Page Objects to perform actions
  - Contains assertions (NUnit `Assert.That(...)`)
  - Maintains scenario state, e.g. `_initialDuration`

> Page Objects should do UI actions; StepDefinitions should do orchestration + assertions.

---



---

### 6) reqnroll.json
Reqnroll configuration file.  
Typically used for:
- binding behaviour
- trace configuration
- runtime options

---

### 7) How to Run Tests
 Visual Studio

- Open solution in Visual Studio
- Build the solution
- Open Test Explorer
- Run all tests or selected tests
