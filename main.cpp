
#include <CoopManager.h>
#include <BlynkSimpleEsp8266.h>

BlynkTimer Timer;
CoopManager Coop;

void CoopWrapper() // Required to easily pass function to timer
{
  Coop.RunCoop(&Coop);
}



void setup()
{
  Serial.begin(9600);
  Coop.Initialize(); 
  digitalWrite(COMMON_LIMIT, HIGH);
  Timer.setInterval(200L, CoopWrapper);
  Blynk.config("9JFsInJEaPBtP995EOHgQ9YQ3XX4pMg5");
   

}

void loop()
{
  Blynk.run();
  Timer.run();
}

// BLYNK STUFF

BLYNK_WRITE(V0)
{
  Coop.SetState(param.asInt());
}

BLYNK_WRITE(V1)
{
  Coop.SetState(param.asInt());
}

BLYNK_WRITE(V2)
{
  Coop.SetState(param.asInt());
}

BLYNK_WRITE(V3)
{
  Coop.SetState(param.asInt());
}

BLYNK_READ(V5)
{
  Blynk.virtualWrite(V5, Coop.CheckBattery());
}

// Embedis


   