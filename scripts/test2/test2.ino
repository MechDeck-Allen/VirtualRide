#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

#define SERVICE_UUID        "12345678-1234-1234-1234-1234567890AB"
#define SPEED_UUID          "12345678-1234-1234-1234-1234567890AC"

BLECharacteristic *speedCharacteristic;

float speed = 0.0;

void setup()
{
    Serial.begin(115200);

    // Start BLE
    BLEDevice::init("VirtualRide");

    // Create BLE server
    BLEServer *server = BLEDevice::createServer();

    // Create service
    BLEService *service =
        server->createService(SERVICE_UUID);

    // Create speed characteristic
    speedCharacteristic =
        service->createCharacteristic(
            SPEED_UUID,
            BLECharacteristic::PROPERTY_READ |
            BLECharacteristic::PROPERTY_NOTIFY
        );

    // Enable notifications
    speedCharacteristic->addDescriptor(
        new BLE2902()
    );

    // Start service
    service->start();

    // Start advertising
    BLEAdvertising *advertising =
        BLEDevice::getAdvertising();

    advertising->addServiceUUID(SERVICE_UUID);
    advertising->setScanResponse(true);

    BLEDevice::startAdvertising();

    Serial.println("VirtualRide BLE started");
}

void loop()
{
    // Simulated speed
    speed += 0.5;

    if (speed > 30.0)
    {
        speed = 0.0;
    }

    // Convert speed to text
    char speedString[10];

    snprintf(
        speedString,
        sizeof(speedString),
        "%.1f",
        speed
    );

    // Put value into BLE characteristic
    speedCharacteristic->setValue(
        speedString
    );

    // Send notification
    speedCharacteristic->notify();

    // Debug output
    Serial.print("Sending speed: ");
    Serial.println(speedString);

    delay(500);
}