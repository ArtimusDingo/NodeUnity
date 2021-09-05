
#include <CoopManager.h>

enum State {OFF = 0, DOOR_OPEN = 1, DOOR_CLOSE = 2, AWAIT = 3, AUTO_OPEN = 4, AUTO_CLOSE = 5};
State state;

CoopManager::CoopManager()
    {
        pinMode(OPEN_PIN, OUTPUT);
        pinMode(CLOSE_PIN, OUTPUT);
        pinMode(BATTERY_PIN, INPUT);
        pinMode(CAMERA_PIN, OUTPUT);
        pinMode(BATTERY_PIN, INPUT);
        pinMode(TRIGGER_PIN, OUTPUT);
        pinMode(ECHO_PIN, INPUT);
        pinMode(COMMON_LIMIT, OUTPUT);
        pinMode(NO_LIMIT, INPUT);
        state = OFF;
    }
void CoopManager::SetState(int _state)
{
   state = static_cast<State>(_state);
}

void CoopManager::Initialize()
{
   // _connectWifi();
  
    state = AWAIT;
}

long CoopManager::CheckBattery()
{
    _battery = (long*)analogRead(BATTERY_PIN);
    long voltage = (*_battery / *_batcalreading) * *_batcalvoltage;
    return voltage;   
}

int readAnalog()
{
    return analogRead(BATTERY_PIN);
}

bool CoopManager::CheckEchoSensor()
{
    digitalWrite(TRIGGER_PIN, LOW);
    delayMicroseconds(2);
    digitalWrite(TRIGGER_PIN, HIGH);
    delayMicroseconds(10);
    digitalWrite(TRIGGER_PIN, LOW);
    long duration = pulseIn(ECHO_PIN, HIGH);
    long distance = (duration*.0343)/2L;
    Serial.print("Distance: ");
    Serial.println(distance);
    return true;
}

void CoopManager::RunCoop(CoopManager* Coop)
{
   switch(state)
   {
       case OFF:
            Coop->_offDoor();
            state = AWAIT;
       break;
       case DOOR_OPEN:
            Coop->_openDoor();
       break;
       case DOOR_CLOSE:
            Coop->_closeDoor();
       break;
       case AWAIT:
          //  Coop->CheckEchoSensor();
          //  Coop->CheckBattery();
      break;
      case AUTO_OPEN:
      break;
      case AUTO_CLOSE:
      break;
   }
}

void CoopManager::_connectWifi()
{   
    Wifi.setAPStaticIPConfig(IPAddress(10,0,1,1), IPAddress(10,0,1,1), IPAddress(255,255,255,0));
    Wifi.autoConnect("ChickyCoop");
}

void CoopManager::_openDoor()
{
    digitalWrite(OPEN_PIN, HIGH);
    digitalWrite(CLOSE_PIN, LOW);
}

void CoopManager::_closeDoor()
{
    if(digitalRead(NO_LIMIT))
    {
        state = OFF;
    }
    digitalWrite(OPEN_PIN, LOW);
    digitalWrite(CLOSE_PIN, HIGH);
    
}

void CoopManager::_offDoor()
{
    digitalWrite(OPEN_PIN, LOW);
    digitalWrite(CLOSE_PIN, LOW);
}


