#include <Arduino.h>

#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>


// ================================================================
// VIRTUALRIDE BLE CONFIGURATION
// ================================================================

#define DEVICE_NAME "VirtualRide"

#define SERVICE_UUID \
    "12345678-1234-1234-1234-1234567890AB"

#define TELEMETRY_UUID \
    "12345678-1234-1234-1234-1234567890AC"

#define COMMAND_UUID \
    "12345678-1234-1234-1234-1234567890AD"


// ================================================================
// HALL SENSOR CONFIGURATION
// ================================================================

// GPIO connected to Hall sensor output.
const int HALL_SENSOR_PIN = 27;


// Number of magnets passing the Hall sensor per revolution.
//
// Physical setup:
// 2 magnets mounted 180 degrees apart.
//
// Therefore:
// 2 Hall pulses = 1 complete shaft revolution.
//
const float MAGNETS_PER_REVOLUTION = 2.0f;


// Hall sensor active level.
//
// Expected behavior:
// Hall output goes LOW when a magnet is detected.
//
const int HALL_ACTIVE_STATE = LOW;


// ================================================================
// SPEED / MECHANICAL CALIBRATION
// ================================================================

// Effective bicycle wheel diameter in metres.
//
// Current assumption:
// 700 mm = 0.700 m
//
// This should eventually be replaced with the ACTUAL rolling
// diameter/circumference of the bicycle wheel for best accuracy.
//
float EFFECTIVE_WHEEL_DIAMETER_M = 0.700f;


// Mechanical transmission:
//
// Bicycle wheel
//      ↓
// 21.2 mm roller
//      ↓
// 12.1 mm roller pulley
//      ↓
// belt
//      ↓
// 22.3 mm NEMA shaft pulley
//
// Motor RPM / Wheel RPM:
//
// = (700 / 21.2) × (12.1 / 22.3)
// ≈ 17.916
//
// Therefore:
//
// Wheel RPM / Motor RPM
// = 1 / 17.916
// ≈ 0.05582
//
// So:
//
// effective wheel RPM = sensor RPM × 0.05582
//
float DRIVE_RATIO = 0.05582f;


// ================================================================
// SENSOR FILTERING
// ================================================================

// Reject pulses arriving faster than this.
//
// 10,000 us = 10 ms.
//
// This prevents very fast electrical noise / false triggering
// from being interpreted as additional Hall pulses.
//
const uint32_t MIN_PULSE_INTERVAL_US = 10000;


// Number of RPM samples averaged together.
//
const int RPM_SAMPLE_COUNT = 5;


// ================================================================
// TELEMETRY
// ================================================================

// BLE telemetry interval.
//
// 100 ms = 10 packets per second.
//
const uint32_t TELEMETRY_INTERVAL_MS = 100;


// Serial debug interval.
//
const uint32_t SERIAL_INTERVAL_MS = 250;


// ================================================================
// ZERO SPEED
// ================================================================

// If no Hall pulse is received for this amount of time,
// RPM is considered zero.
//
const uint32_t ZERO_SPEED_TIMEOUT_MS = 1500;


// ================================================================
// SESSION DISTANCE
// ================================================================

// Stored internally in metres.
//
double distanceMeters = 0.0;


// ================================================================
// HALL SENSOR STATE
// ================================================================

volatile uint32_t lastPulseTimeUs = 0;

volatile uint32_t pulseIntervalUs = 0;

volatile bool newPulse = false;


// ================================================================
// RPM FILTER
// ================================================================

float rpmSamples[RPM_SAMPLE_COUNT] = {0};

int rpmSampleIndex = 0;

int validRpmSamples = 0;

float currentRPM = 0.0f;


// ================================================================
// SPEED
// ================================================================

float currentSpeedKmh = 0.0f;


// ================================================================
// BLE
// ================================================================

BLEServer *bleServer = nullptr;

BLECharacteristic *telemetryCharacteristic = nullptr;

BLECharacteristic *commandCharacteristic = nullptr;

volatile bool deviceConnected = false;


// ================================================================
// TIMERS
// ================================================================

uint32_t lastTelemetryTime = 0;

uint32_t lastSerialTime = 0;

uint32_t lastDistanceUpdateTime = 0;


// ================================================================
// HALL SENSOR INTERRUPT
// ================================================================

void IRAM_ATTR hallSensorISR()
{
    uint32_t nowUs = micros();

    uint32_t interval =
        nowUs - lastPulseTimeUs;


    // Reject electrical noise / double triggering.
    if (interval < MIN_PULSE_INTERVAL_US)
    {
        return;
    }


    lastPulseTimeUs = nowUs;

    pulseIntervalUs = interval;

    newPulse = true;
}


// ================================================================
// CLEAR RPM FILTER
// ================================================================

void clearRPMFilter()
{
    for (
        int i = 0;
        i < RPM_SAMPLE_COUNT;
        i++
    )
    {
        rpmSamples[i] = 0.0f;
    }


    rpmSampleIndex = 0;

    validRpmSamples = 0;

    currentRPM = 0.0f;
}


// ================================================================
// RESET RIDE SESSION
// ================================================================

void resetRideSession()
{
    distanceMeters = 0.0;

    currentRPM = 0.0f;

    currentSpeedKmh = 0.0f;

    clearRPMFilter();

    lastDistanceUpdateTime =
        millis();


    Serial.println();
    Serial.println("================================");
    Serial.println("RIDE SESSION RESET");
    Serial.println("Distance: 0.000 km");
    Serial.println("RPM:      0.0");
    Serial.println("Speed:    0.0 km/h");
    Serial.println("================================");
    Serial.println();
}


// ================================================================
// BLE SERVER CALLBACKS
// ================================================================

class VirtualRideServerCallbacks
    : public BLEServerCallbacks
{
    void onConnect(
        BLEServer *server
    ) override
    {
        deviceConnected = true;


        Serial.println();
        Serial.println("================================");
        Serial.println("BLE CLIENT CONNECTED");
        Serial.println("================================");
    }


    void onDisconnect(
        BLEServer *server
    ) override
    {
        deviceConnected = false;


        Serial.println();
        Serial.println("================================");
        Serial.println("BLE CLIENT DISCONNECTED");
        Serial.println("Restarting advertising...");
        Serial.println("================================");


        // Immediately advertise again so the phone can reconnect.
        server->startAdvertising();
    }
};


// ================================================================
// COMMAND CHARACTERISTIC CALLBACK
// ================================================================

class VirtualRideCommandCallbacks
    : public BLECharacteristicCallbacks
{
    void onWrite(
        BLECharacteristic *characteristic
    ) override
    {
        String command =
            characteristic->getValue();

        command.trim();

        if (command.length() == 0)
        {
            return;
        }

        command.toUpperCase();

        Serial.print(
            "BLE COMMAND: "
        );

        Serial.println(
            command
        );


        // --------------------------------------------------------
        // RESET
        // --------------------------------------------------------

        if (
            command == "RESET"
        )
        {
            resetRideSession();

            return;
        }


        // --------------------------------------------------------
        // UNKNOWN COMMAND
        // --------------------------------------------------------

        Serial.println(
            "Unknown BLE command."
        );
    }
};


// ================================================================
// RPM SAMPLE
// ================================================================

void addRPMSample(
    float rpm
)
{
    rpmSamples[rpmSampleIndex] =
        rpm;


    rpmSampleIndex++;


    if (
        rpmSampleIndex >=
        RPM_SAMPLE_COUNT
    )
    {
        rpmSampleIndex = 0;
    }


    if (
        validRpmSamples <
        RPM_SAMPLE_COUNT
    )
    {
        validRpmSamples++;
    }
}


// ================================================================
// AVERAGE RPM
// ================================================================

float getAverageRPM()
{
    if (
        validRpmSamples == 0
    )
    {
        return 0.0f;
    }


    float total = 0.0f;


    for (
        int i = 0;
        i < validRpmSamples;
        i++
    )
    {
        total +=
            rpmSamples[i];
    }


    return
        total /
        validRpmSamples;
}


// ================================================================
// PROCESS HALL SENSOR
// ================================================================

void processHallSensor()
{
    if (!newPulse)
    {
        return;
    }


    noInterrupts();

    uint32_t intervalUs =
        pulseIntervalUs;

    newPulse = false;

    interrupts();


    if (
        intervalUs == 0
    )
    {
        return;
    }


    float intervalSeconds =
        intervalUs /
        1000000.0f;


    // ------------------------------------------------------------
    // Calculate actual NEMA shaft RPM
    // ------------------------------------------------------------
    //
    // With TWO magnets:
    //
    // 2 pulses = 1 revolution
    //
    // Therefore:
    //
    // shaft RPM =
    //
    // 60 / (time between pulses × 2)
    //
    float sensorRPM =
        60.0f /
        (
            intervalSeconds *
            MAGNETS_PER_REVOLUTION
        );


    // ------------------------------------------------------------
    // Convert NEMA shaft RPM → bicycle wheel RPM
    // ------------------------------------------------------------

    float effectiveRPM =
        sensorRPM *
        DRIVE_RATIO;


    // ------------------------------------------------------------
    // RPM smoothing
    // ------------------------------------------------------------

    addRPMSample(
        effectiveRPM
    );


    currentRPM =
        getAverageRPM();
}


// ================================================================
// CALCULATE SPEED
// ================================================================

void calculateSpeed()
{
    // Bicycle wheel circumference.
    float circumference =
        PI *
        EFFECTIVE_WHEEL_DIAMETER_M;


    // metres per minute.
    float metersPerMinute =
        currentRPM *
        circumference;


    // Convert m/min to km/h.
    currentSpeedKmh =
        (
            metersPerMinute *
            60.0f
        ) /
        1000.0f;
}


// ================================================================
// UPDATE DISTANCE
// ================================================================

void updateDistance()
{
    uint32_t nowMs =
        millis();


    if (
        lastDistanceUpdateTime == 0
    )
    {
        lastDistanceUpdateTime =
            nowMs;

        return;
    }


    uint32_t elapsedMs =
        nowMs -
        lastDistanceUpdateTime;


    lastDistanceUpdateTime =
        nowMs;


    if (
        currentSpeedKmh <= 0.0f
    )
    {
        return;
    }


    // km/h × elapsed hours
    double distanceKm =
        currentSpeedKmh *
        (
            elapsedMs /
            3600000.0
        );


    distanceMeters +=
        distanceKm *
        1000.0;
}


// ================================================================
// ZERO SPEED DETECTION
// ================================================================

void updateZeroSpeed()
{
    uint32_t nowUs =
        micros();


    noInterrupts();

    uint32_t lastPulse =
        lastPulseTimeUs;

    interrupts();


    if (
        lastPulse == 0
    )
    {
        currentRPM = 0.0f;

        currentSpeedKmh = 0.0f;

        return;
    }


    uint32_t elapsedUs =
        nowUs -
        lastPulse;


    if (
        elapsedUs >
        (
            ZERO_SPEED_TIMEOUT_MS *
            1000UL
        )
    )
    {
        currentRPM = 0.0f;

        currentSpeedKmh = 0.0f;

        clearRPMFilter();
    }
}


// ================================================================
// SEND TELEMETRY
// ================================================================

void sendTelemetry()
{
    if (
        !deviceConnected
    )
    {
        return;
    }


    char telemetry[64];


    // Exact protocol expected by Unity:
    //
    // VR1,SPEED,RPM,DISTANCE
    //
    // Example:
    //
    // VR1,18.6,74.0,2.430
    //

    snprintf(
        telemetry,
        sizeof(telemetry),
        "VR1,%.1f,%.1f,%.3f",
        currentSpeedKmh,
        currentRPM,
        distanceMeters / 1000.0
    );


    telemetryCharacteristic->setValue(
        telemetry
    );


    telemetryCharacteristic->notify();
}


// ================================================================
// SERIAL DEBUG
// ================================================================

void printSerialData()
{
    Serial.print(
        "Speed: "
    );

    Serial.print(
        currentSpeedKmh,
        1
    );


    Serial.print(
        " km/h | RPM: "
    );

    Serial.print(
        currentRPM,
        1
    );


    Serial.print(
        " | Distance: "
    );

    Serial.print(
        distanceMeters / 1000.0,
        3
    );


    Serial.print(
        " km | BLE: "
    );


    if (
        deviceConnected
    )
    {
        Serial.println(
            "CONNECTED"
        );
    }
    else
    {
        Serial.println(
            "DISCONNECTED"
        );
    }
}


// ================================================================
// START BLE
// ================================================================

void startBLE()
{
    BLEDevice::init(
        DEVICE_NAME
    );


    bleServer =
        BLEDevice::createServer();


    bleServer->setCallbacks(
        new VirtualRideServerCallbacks()
    );


    BLEService *service =
        bleServer->createService(
            SERVICE_UUID
        );


    // ============================================================
    // TELEMETRY CHARACTERISTIC
    // ============================================================

    telemetryCharacteristic =
        service->createCharacteristic(
            TELEMETRY_UUID,
            BLECharacteristic::PROPERTY_READ |
            BLECharacteristic::PROPERTY_NOTIFY
        );


    telemetryCharacteristic->addDescriptor(
        new BLE2902()
    );


    // ============================================================
    // COMMAND CHARACTERISTIC
    // ============================================================

    commandCharacteristic =
        service->createCharacteristic(
            COMMAND_UUID,
            BLECharacteristic::PROPERTY_WRITE
        );


    commandCharacteristic->setCallbacks(
        new VirtualRideCommandCallbacks()
    );


    // ============================================================
    // START SERVICE
    // ============================================================

    service->start();


    // ============================================================
    // ADVERTISING
    // ============================================================

    BLEAdvertising *advertising =
        BLEDevice::getAdvertising();


    advertising->addServiceUUID(
        SERVICE_UUID
    );


    advertising->setScanResponse(
        true
    );


    BLEDevice::startAdvertising();


    Serial.println();
    Serial.println("================================");
    Serial.println("VirtualRide BLE READY");
    Serial.println("Device: VirtualRide");
    Serial.println("Advertising...");
    Serial.println("================================");
}


// ================================================================
// SETUP
// ================================================================

void setup()
{
    Serial.begin(
        115200
    );


    delay(500);


    Serial.println();
    Serial.println("================================");
    Serial.println("VirtualRide ESP32");
    Serial.println("Hall Sensor + BLE Telemetry");
    Serial.println("================================");


    // ------------------------------------------------------------
    // Hall sensor
    // ------------------------------------------------------------

    pinMode(
        HALL_SENSOR_PIN,
        INPUT_PULLUP
    );


    // TWO magnets:
    //
    // 2 magnets × 1 falling edge per magnet
    // = 2 pulses per shaft revolution
    //
    attachInterrupt(
        digitalPinToInterrupt(
            HALL_SENSOR_PIN
        ),
        hallSensorISR,
        FALLING
    );


    Serial.print(
        "Hall sensor GPIO: "
    );


    Serial.println(
        HALL_SENSOR_PIN
    );


    Serial.print(
        "Magnets/revolution: "
    );


    Serial.println(
        MAGNETS_PER_REVOLUTION
    );


    Serial.print(
        "Wheel diameter: "
    );


    Serial.print(
        EFFECTIVE_WHEEL_DIAMETER_M,
        4
    );


    Serial.println(
        " m"
    );


    Serial.print(
        "Drive ratio: "
    );


    Serial.println(
        DRIVE_RATIO,
        5
    );


    // ------------------------------------------------------------
    // BLE
    // ------------------------------------------------------------

    startBLE();


    // ------------------------------------------------------------
    // Distance timer
    // ------------------------------------------------------------

    lastDistanceUpdateTime =
        millis();


    Serial.println();

    Serial.println(
        "Waiting for Hall pulses..."
    );
}


// ================================================================
// LOOP
// ================================================================

void loop()
{
    processHallSensor();

    updateZeroSpeed();

    calculateSpeed();

    updateDistance();


    uint32_t nowMs =
        millis();


    // ------------------------------------------------------------
    // BLE telemetry
    // ------------------------------------------------------------

    if (
        nowMs -
        lastTelemetryTime >=
        TELEMETRY_INTERVAL_MS
    )
    {
        lastTelemetryTime =
            nowMs;

        sendTelemetry();
    }


    // ------------------------------------------------------------
    // Serial
    // ------------------------------------------------------------

    if (
        nowMs -
        lastSerialTime >=
        SERIAL_INTERVAL_MS
    )
    {
        lastSerialTime =
            nowMs;

        printSerialData();
    }


    delay(1);
}