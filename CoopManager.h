#ifndef CoopManager_h
#define CoopManager_h

#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WiFiManager.h>
#include <DoorPins.h>


class CoopManager
{
    public:
        CoopManager();
        static void RunCoop(CoopManager*);
        void Initialize();
        void SetState(int);
        long CheckBattery();    
        bool CheckEchoSensor();
        int readAnalog();
        void SetCalVoltage();
    private:   
        WiFiManager Wifi;
        void _connectWifi();
        void _openDoor();
        void _closeDoor();
        void _offDoor();
        const char * authtoken;
        long* _battery;
        long*_batcalreading;
        long* _batcalvoltage;
        long* _triggerDuration;
        const long full = 2.54L; // 12.6 volts divided by 5, the factor of the voltage divider
        const long half = 2.436L; // 12.18 volts divided by 5, the factor of the voltage divider
};

#endif