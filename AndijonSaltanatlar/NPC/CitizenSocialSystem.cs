using AndijonSaltanatlar.Units;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.NPC
{
    public sealed class CitizenSocialSystem
    {
        private static readonly string[,] Dialogues =
        {
            { "Assalom!", "Va alaykum!" },
            { "Bozor gavjum!", "Savdo barakali!" },
            { "Hosil mo'l!", "Baraka toping!" }
        };

        private float _nextConversation = 2f;
        private int _dialogueIndex;

        public void Update(UnitManager units, float dt)
        {
            foreach (Unit unit in units.Units)
                if (unit is CitizenUnit citizen) citizen.UpdateSpeech(dt);

            _nextConversation -= dt;
            if (_nextConversation > 0f) return;
            _nextConversation = 4f;

            CitizenUnit first = null;
            CitizenUnit second = null;
            float closest = 2.4f;
            foreach (Unit unit in units.Units)
            {
                if (!(unit is CitizenUnit citizen) || citizen.IsDown || citizen.ExternallyControlled) continue;
                foreach (Unit other in units.Units)
                {
                    if (!(other is CitizenUnit nearby) || nearby == citizen || nearby.IsDown || nearby.ExternallyControlled) continue;
                    float distance = Vector2.Distance(citizen.Position, nearby.Position);
                    if (distance < closest)
                    {
                        closest = distance;
                        first = citizen;
                        second = nearby;
                    }
                }
            }

            if (first == null) return;
            first.Say(Dialogues[_dialogueIndex, 0]);
            second.Say(Dialogues[_dialogueIndex, 1]);
            _dialogueIndex = (_dialogueIndex + 1) % Dialogues.GetLength(0);
        }
    }
}
