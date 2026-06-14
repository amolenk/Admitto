FROM quay.io/keycloak/keycloak:26.6

USER root

RUN mkdir -p /opt/keycloak/data/import /opt/keycloak/themes/admitto \
    && chown -R 1000:0 /opt/keycloak/data /opt/keycloak/themes \
    && chmod -R g=u /opt/keycloak/data /opt/keycloak/themes

COPY --chown=1000:0 AdmittoRealm.Deployment.json /opt/keycloak/data/import/admitto-realm.json
COPY --chown=1000:0 themes/admitto /opt/keycloak/themes/admitto

RUN chmod 0755 /opt/keycloak/data /opt/keycloak/data/import \
    && chmod 0644 /opt/keycloak/data/import/admitto-realm.json \
    && chmod -R a+rX /opt/keycloak/themes/admitto

USER 1000

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start", "--import-realm"]
