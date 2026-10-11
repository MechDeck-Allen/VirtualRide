#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

void setup()
{
    Serial.begin(115200);

    BLEDevice::init("VirtualRide");

    BLEServer *server = BLEDevice::createServer();

    BLEService *service =
        server->createService(
            "12345678-1234-1234-1234-1234567890AB"
        );

    BLECharacteristic *speedCharacteristic =
        service->createCharacteristic(
            "12345678-1234-1234-1234-1234567890AC",
            BLECharacteristic::PROPERTY_READ |
            BLECharacteristic::PROPERTY_NOTIFY
        );

    speedCharacteristic->addDescriptor(
        new BLE2902()
    );

    service->start();

    BLEAdvertising *advertising =
        BLEDevice::getAdvertising();

    advertising->addServiceUUID(
        "12345678-1234-1234-1234-1234567890AB"
    );

    advertising->setScanResponse(true);

    BLEDevice::startAdvertising();

    Serial.println("VirtualRide BLE started");
}

void loop()
{
}